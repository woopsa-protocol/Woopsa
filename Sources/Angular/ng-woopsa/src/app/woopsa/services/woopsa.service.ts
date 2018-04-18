import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';

import {
  WoopsaClient, WoopsaMetaResult, WoopsaValue,
  WoopsaSubscriptionChannel, WoopsaSubscription
} from './woopsa';

@Injectable()
export class WoopsaService {
  protected client: WoopsaClient;
  protected defaultChannel: WoopsaSubscriptionChannel;

  constructor(private http: HttpClient) {
    this.client = new WoopsaClient(http);
    this.defaultChannel = this.createChannel(200);
  }

  setUrl(path: string) {
    this.client.setUrl(path);
  }

  setAuthorization(username: string, password: string) {
    this.client.setAuthorization(username, password);
  }

  meta(path: string = ''): Promise<WoopsaMetaResult> {
    return this.client.meta(path);
  }

  read(path: string): Promise<WoopsaValue> {
    return this.client.read(path);
  }

  write(path: string, value: any) {
    return this.client.write(path, value);
  }

  invoke(path: string, args: any = {}, forceNoArgsSerialize = false): Promise<WoopsaValue> {
    return this.client.invoke(path, args, forceNoArgsSerialize);
  }

  createChannel(size: number): WoopsaSubscriptionChannel {
    return new WoopsaSubscriptionChannel(this.client, size);
  }

  subscribe(path: string, channel: WoopsaSubscriptionChannel = this.defaultChannel,
            monitorInterval: number = 0.02, publishInterval: number = 0.05): Promise<WoopsaSubscription> {
    return this.client.subscribe(channel, path, monitorInterval, publishInterval);
  }

  unsubscribe(subscription: WoopsaSubscription, channel: WoopsaSubscriptionChannel = this.defaultChannel): Promise<void> {
    return this.client.unsubscribe(channel, subscription);
  }
}
