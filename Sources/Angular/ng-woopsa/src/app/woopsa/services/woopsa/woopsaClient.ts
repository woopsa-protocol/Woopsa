import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Http, Response, Headers, RequestOptions } from '@angular/http';

import { Observable } from 'rxjs/Observable';
import { Subject } from 'rxjs/Subject';

import { WoopsaValue } from './woopsaValue';
import { WoopsaValueType } from './woopsaType';
import { WoopsaRequest } from './woopsaRequest';
import { ILiteEvent, LiteEvent } from './lite-event';

export class WoopsaClient {
  public readonly verbRead: string = 'read';
  public readonly verbWrite: string = 'write';
  public readonly verbInvoke: string = 'invoke';
  public readonly verbMeta: string = 'meta';
  private baseUrl: string;
  private username: string;
  private password: string;
  private offline = false;
  private _isLastCommunicationSuccessful = true;
  private readonly _onIsLastCommunicationSuccessfulChange = new LiteEvent<boolean>();

  constructor(private http: HttpClient) {
  }

  static mapValue(readResult: WoopsaReadResult): WoopsaValue {
    let value: WoopsaValue;
    const valueType: WoopsaValueType = WoopsaValueType[readResult.Type];
    const timeStamp: number = readResult.TimeStamp !== null ? NaN : Date.parse(readResult.TimeStamp);

    if (valueType === WoopsaValueType.JsonData) {
      value = WoopsaValue.CreateChecked(readResult.Value, valueType, timeStamp);
    } else {
      value = WoopsaValue.CreateChecked(String(readResult.Value), valueType, timeStamp);
    }

    return value;
  }

  get onIsLastCommunicationSuccessfulChange(): ILiteEvent<boolean> {
    return this._onIsLastCommunicationSuccessfulChange.expose();
  }

  get isLastCommunicationSuccessful(): boolean {
    return this._isLastCommunicationSuccessful;
  }

  goOffline() {
    this.offline = true;
  }

  goOnline() {
    this.offline = false;
  }

  setUrl(path: string): void {
    this.baseUrl = path;
  }

  setAuthorization(username: string, password: string): void {
    this.username = username;
    this.password = password;
  }

  meta(path: string = ''): Promise<WoopsaMetaResult> {
    return this.sendRequest(this.getMetaRequest(path));
  }

  getMetaRequest(path: string = ''): WoopsaRequest {
    return this.getRequest(path, this.verbMeta);
  }

  sendMetaRequest(metaRequest: WoopsaRequest): Promise<WoopsaMetaResult> {
    return this.sendRequest(metaRequest);
  }

  read(path: string): Promise<WoopsaValue> {
    return this.sendReadRequest(this.getReadRequest(path));
  }

  write(path: string, value: any): Promise<WoopsaValue> {
    const url = `${this.baseUrl}/${this.verbWrite}/${path}`;
    const body = `Value=${value}`;
    let headers = new HttpHeaders();
    headers = headers.set('Content-Type', 'application/x-www-form-urlencoded; charset=UTF-8');

    return this.http.post(url, body, { headers: headers })
      .toPromise()
      .then(readResult => WoopsaClient.mapValue(<WoopsaReadResult>readResult))
      .catch(error => {
        this.updateIsLastCommunicationSuccessful(false);
        throw error;
      });
  }

  getReadRequest(path: string): WoopsaRequest {
    return this.getRequest(path, this.verbRead);
  }

  sendReadRequest(readRequest: WoopsaRequest): Promise<WoopsaValue> {
    return this.sendRequest(readRequest).then(readResult => WoopsaClient.mapValue(<WoopsaReadResult>readResult));
  }

  invoke(path: string, args: any = {}, forceNoArgsSerialize = false): Promise<WoopsaValue> {
    return this.sendInvokeRequest(this.getInvokeRequest(path, args, forceNoArgsSerialize));
  }

  getInvokeRequest(path: string, args: any = {}, forceNoArgsSerialize = false): WoopsaRequest {
    return this.getRequest(path, this.verbInvoke, args, forceNoArgsSerialize);
  }

  sendInvokeRequest(invokeRequest: WoopsaRequest): Promise<WoopsaValue> {
    return this.sendRequest(invokeRequest).then(response => {
      if (response) {
        const readResult = <WoopsaReadResult>response;
        if (readResult != null) {
          return WoopsaClient.mapValue(readResult);
        }
      }
      return null;
    });
  }

  subscribe(channel: WoopsaSubscriptionChannel, path: string,
    monitorInterval: number = 0.02, publishInterval: number = 0.05): Promise<WoopsaSubscription> {
    const subscription = new WoopsaSubscription(path, monitorInterval, publishInterval);
    return channel.create().then(channelId => channel.register(subscription));
  }

  unsubscribe(channel: WoopsaSubscriptionChannel, subscription: WoopsaSubscription): Promise<void> {
    return channel.unregister(subscription);
  }

  unsubscribeOffline(channel: WoopsaSubscriptionChannel, subscription: WoopsaSubscription) {
    channel.unregisterOffline(subscription);
  }

  private getRequest(path: string, verb: string, args: any = {}, forceNoArgsSerialize = false): WoopsaRequest {
    let headers = new HttpHeaders();
    let body = '';
    if (verb === this.verbInvoke) {
      headers = headers.set('Content-Type', 'application/x-www-form-urlencoded; charset=UTF-8');
      if (!forceNoArgsSerialize) {
        let first = true;
        for (const propertyKey in args) {
          if (!args.hasOwnProperty(propertyKey)) {
            continue;
          }

          if (first) {
            first = false;
          } else {
            body += '&';
          }

          body += `${propertyKey}=${encodeURIComponent(args[propertyKey])}`;
        }
      } else {
        body = args;
      }
    } else if (verb === this.verbWrite) {
      body = args;
    }
    headers = this.addAuthorizationHeader(headers);
    return new WoopsaRequest(`${this.baseUrl}/${verb}/${path}`, verb, headers, body);
  }

  private sendRequest(request: WoopsaRequest): Promise<any> {
    if (this.offline) {
      this.updateIsLastCommunicationSuccessful(false);
      const errorMessage = 'Network is offline [simulation]';
      return Promise.reject(errorMessage);
    }
    if (request.verb === this.verbRead || request.verb === this.verbMeta) {
      return this.http.get(
        request.url,
        { headers: request.headers }).toPromise().then(response => {
          this.updateIsLastCommunicationSuccessful(true);
          return response;
        }).catch(error => {
          this.updateIsLastCommunicationSuccessful(false);
          throw error;
        });
    } else if (request.verb === this.verbInvoke || request.verb === this.verbWrite) {
      return this.http.post(
        request.url,
        request.body,
        { headers: request.headers }).toPromise().then(response => {
          this.updateIsLastCommunicationSuccessful(true);
          return response;
        }).catch(error => {
          this.updateIsLastCommunicationSuccessful(false);
          throw error;
        });
    }
  }

  private addAuthorizationHeader(headers: HttpHeaders): HttpHeaders {
    return headers.set('Authorization', 'Basic ' + btoa(`${this.username}:${this.password}`));
  }

  private updateIsLastCommunicationSuccessful(value: boolean) {
    if (this._isLastCommunicationSuccessful !== value) {
      this._isLastCommunicationSuccessful = value;
      this._onIsLastCommunicationSuccessfulChange.trigger(value);
    }
  }
}

export class WoopsaSubscription {

  constructor(path: string, monitorInterval: number, publishInterval: number) {
      this.path = path;
      this.monitorInterval = monitorInterval;
      this.publishInterval = publishInterval;
  }

  readonly path: string;
  readonly monitorInterval: number;
  readonly publishInterval: number;

  id: number;
  subject: Subject<WoopsaValue> = new Subject<WoopsaValue>();
  changes: Observable<WoopsaValue> = this.subject.asObservable();
}

export class WoopsaSubscriptionChannel {
  private readonly subscriptionCreate: string = 'SubscriptionService/CreateSubscriptionChannel';
  private readonly subscriptionRegister: string = 'SubscriptionService/RegisterSubscription';
  private readonly subscriptionUnregister: string = 'SubscriptionService/UnregisterSubscription';
  private readonly subscriptionWait: string = 'SubscriptionService/WaitNotification';

  private waitRunning: boolean;

  private lastNotificationId: number;
  private subscriptions: Map<string, WoopsaSubscription>;
  private channelCreationPromise: Promise<number>;

  ChannelId: number;

  constructor(private _client: WoopsaClient,
      private _size: number) {
      this.reset();
  }

  create(): Promise<number> {
      if (!this.channelCreationPromise) {
          // TODO : improve woopsa value to parse all supported types.
          this.channelCreationPromise = this._client.invoke(this.subscriptionCreate, {NotificationQueueSize: this._size})
              .then(channelId => this.ChannelId = +channelId.asText);
      }
      return this.channelCreationPromise;
  }

  reset() {
      this.channelCreationPromise = undefined;
      this.lastNotificationId = 0;
      this.subscriptions = new Map<string, WoopsaSubscription>();
      this.waitRunning = false;
  }

  register(subscription: WoopsaSubscription): Promise<WoopsaSubscription> {
      const args = {
          SubscriptionChannel: this.ChannelId,
          PropertyLink: subscription.path,
          MonitorInterval: subscription.monitorInterval,
          PublishInterval: subscription.publishInterval
      };
      return this._client.invoke(this.subscriptionRegister, args)
          .then(subscriptionId => {
              subscription.id = +subscriptionId.asText;
              this.subscriptions.set(`${subscription.id}`, subscription);
              this.tryWait();
              return subscription;
          });
  }

  unregister(subscription: WoopsaSubscription): Promise<void> {
      if (this.subscriptions.has(`${subscription.id}`)) {
        const args = {
              SubscriptionChannel: this.ChannelId,
              SubscriptionId: subscription.id
          };
          return this._client.invoke(this.subscriptionUnregister, args)
              .then(success => {
                  if (success.asText === 'true') {
                      this.subscriptions.delete(`${subscription.id}`);
                  }
              });
      } else {
          return Promise.resolve();
      }
  }

  unregisterOffline(subscription: WoopsaSubscription) {
      if (this.subscriptions.has(`${subscription.id}`)) {
          this.subscriptions.delete(`${subscription.id}`);
      }
  }

  private tryWait() {
      if (!this.waitRunning) {
          this.waitRunning = true;
          this.wait();
      } else {
          return;
      }
  }

  private wait() {
      const args = {
          SubscriptionChannel: this.ChannelId,
          LastNotificationId: this.lastNotificationId
      };
      this._client.invoke(this.subscriptionWait, args)
          .then(result => {
              const notifications = <any>result.asText;
              console.log(`[${this.ChannelId}] Received notifications : ${notifications}`);

              for (const n of notifications) {
                  if (this.subscriptions.has(`${n.SubscriptionId}`)) {
                      this.subscriptions.get(`${n.SubscriptionId}`).subject.next(WoopsaClient.mapValue(n.Value));
                      console.log('Sent notification : ' + n.Value);
                      if (n.Id > this.lastNotificationId) {
                          this.lastNotificationId = n.Id;
                      }
                  }
              }

              if (this.subscriptions.size > 0) {
                  // TODO: check if disposing is required.
                  this.wait();
              } else {
                  this.waitRunning = false;
              }
          }).catch(error => {
              console.log('Subscription error');
          });
  }
}

export class WoopsaMetaResult {
  Name: string;
  Items: string[];
  Properties: WoopsaPropertyMeta[];
  Methods: WoopsaMethodMeta[];
}

export class WoopsaPropertyMeta {
  Name: string;
  Type: string;
  ReadOnly: boolean;
}

export class WoopsaMethodMeta {
  Name: string;
  ReturnType: string;
  ArgumentInfos: WoopsaMethodArgumentInfoMeta[];
}

export class WoopsaMethodArgumentInfoMeta {
  Name: string;
  Type: string;
}

export class WoopsaReadResult {
  Value: any;
  Type: string;
  TimeStamp: string;
}
