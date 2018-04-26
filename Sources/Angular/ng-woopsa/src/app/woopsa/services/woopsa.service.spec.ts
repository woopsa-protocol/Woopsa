import { TestBed, inject } from '@angular/core/testing';

import { WoopsaService } from './woopsa.service';

describe('WoopsaService', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [WoopsaService]
    });
  });

  it('should be created', inject([WoopsaService], (service: WoopsaService) => {
    expect(service).toBeTruthy();
  }));
});
