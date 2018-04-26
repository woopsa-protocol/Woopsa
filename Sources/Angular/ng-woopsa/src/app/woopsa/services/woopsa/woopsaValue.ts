import { WoopsaValueType } from './woopsaType';
import { IWoopsaValue } from './woopsaInterfaces';

// TODO: Add jsondata support
export class WoopsaValue implements IWoopsaValue {
  asText: string;
  type: WoopsaValueType;
  timestamp: number;
  // private jsonData : WoopsaJsonData = null;

  constructor(text: string, type: WoopsaValueType, timestamp: number = NaN) {
    this.asText = text;
    this.type = type;
    this.timestamp = timestamp;

    // if(type == WoopsaValueType.JsonData)
    //  jsonData = Woopsa.WoopsaJsonData.CreateFromText(text);
  }

  static CreateUnchecked(text: string, type: WoopsaValueType, timestamp: number = NaN): WoopsaValue {
    return new WoopsaValue(text, type, timestamp);
  }

  static CreateChecked(text: string, type: WoopsaValueType, timestamp: number = NaN): WoopsaValue {
    try {
      // TODO: realize check
    } catch (error) { }
    return WoopsaValue.CreateUnchecked(text, type, timestamp);
  }
}
