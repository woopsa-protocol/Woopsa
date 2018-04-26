import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClientModule } from '@angular/common/http';

import { WoopsaService } from './services/woopsa.service';

@NgModule({
  imports: [
    CommonModule,
    HttpClientModule
  ],
  declarations: [],
  providers: [ WoopsaService ]
})
export class WoopsaModule { }

export * from './services/woopsa';
export { WoopsaService } from './services/woopsa.service';
