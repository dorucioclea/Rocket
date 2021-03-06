import { Component, forwardRef, Input, OnInit } from '@angular/core';
import { FormsModule, NG_VALUE_ACCESSOR } from '@angular/forms';
import { createHostFactory, SpectatorHost } from '@ngneat/spectator/jest';
import { AbstractNgModelComponent } from '../abstracts';
import { timer } from 'rxjs';

@Component({
  selector: 'rocket-test',
  template: '',
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      // tslint:disable-next-line: no-forward-ref
      useExisting: forwardRef(() => TestComponent),
      multi: true,
    },
  ],
})
export class TestComponent extends AbstractNgModelComponent implements OnInit {
  @Input() override: boolean;

  ngOnInit() {
    setTimeout(() => {
      if (this.override) {
        this.value = 'test';
      }
    }, 0);
  }
}

describe('AbstractNgModelComponent', () => {
  let spectator: SpectatorHost<TestComponent, { val: any; override: boolean }>;

  const createHost = createHostFactory({
    component: TestComponent,
    declarations: [AbstractNgModelComponent],
    imports: [FormsModule],
  });

  beforeEach(() => {
    spectator = createHost('<rocket-test [(ngModel)]="val" [override]="override"></rocket-test>', {
      hostProps: {
        val: '1',
        override: false,
      },
    });
  });

  test('should pass the value with ngModel', done => {
    timer(0).subscribe(() => {
      expect(spectator.component.value).toBe('1');
      done();
    });
  });

  test('should set the value with ngModel', done => {
    spectator.setHostInput({ val: '2', override: true });

    timer(0).subscribe(() => {
      expect(spectator.hostComponent.val).toBe('test');
      done();
    });
  });
});
