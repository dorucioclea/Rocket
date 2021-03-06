import { Component, EventEmitter, Inject, Input, OnInit, Optional, Output } from '@angular/core';
import { createDirectiveFactory, SpectatorDirective } from '@ngneat/spectator/jest';
import { Store } from '@ngxs/store';
import { Subject } from 'rxjs';
import { ReplaceableTemplateDirective } from '../directives';
import { ReplaceableComponents } from '../models';

@Component({
  selector: 'rocket-default-component',
  template: `
    <p>default</p>
  `,
  exportAs: 'rocketDefaultComponent',
})
class DefaultComponent implements OnInit {
  @Input()
  oneWay;

  @Input()
  twoWay: boolean;

  @Output()
  readonly twoWayChange = new EventEmitter<boolean>();

  @Output()
  readonly someOutput = new EventEmitter<string>();

  ngOnInit() {}

  setTwoWay(value) {
    this.twoWay = value;
    this.twoWayChange.emit(value);
  }
}

@Component({
  selector: 'rocket-external-component',
  template: `
    <p>external</p>
  `,
})
class ExternalComponent {
  constructor(
    @Optional()
    @Inject('REPLACEABLE_DATA')
    public data: ReplaceableComponents.ReplaceableTemplateData<any, any>,
  ) {}
}

describe('ReplaceableTemplateDirective', () => {
  const selectResponse = new Subject();
  const mockSelect = jest.fn(() => selectResponse);

  let spectator: SpectatorDirective<ReplaceableTemplateDirective>;
  const createDirective = createDirectiveFactory({
    directive: ReplaceableTemplateDirective,
    providers: [{ provide: Store, useValue: { select: mockSelect } }],
    declarations: [DefaultComponent, ExternalComponent],
    entryComponents: [ExternalComponent],
  });

  describe('without external component', () => {
    const twoWayChange = jest.fn(a => a);
    const someOutput = jest.fn(a => a);

    beforeEach(() => {
      spectator = createDirective(
        `
        <div *rocketReplaceableTemplate="{inputs: {oneWay: {value: oneWay}, twoWay: {value: twoWay, twoWay: true}}, outputs: {twoWayChange: twoWayChange, someOutput: someOutput}, componentKey: 'TestModule.TestComponent'}; let initTemplate = initTemplate">
          <rocket-default-component #defaultComponent="rocketDefaultComponent"></rocket-default-component>
        </div>
        `,
        { hostProps: { oneWay: { label: 'Test' }, twoWay: false, twoWayChange, someOutput } },
      );
      selectResponse.next(undefined);
      const component = spectator.query(DefaultComponent);
      spectator.directive.context.initTemplate(component);
      spectator.detectChanges();
    });

    afterEach(() => twoWayChange.mockClear());

    it('should display the default template when store response is undefined', () => {
      expect(spectator.query('rocket-default-component')).toBeTruthy();
    });

    it('should be setted inputs and outputs', () => {
      const component = spectator.query(DefaultComponent);
      expect(component.oneWay).toEqual({ label: 'Test' });
      expect(component.twoWay).toEqual(false);
    });

    it('should change the component inputs', () => {
      const component = spectator.query(DefaultComponent);
      spectator.setHostInput({ oneWay: 'test' });
      component.setTwoWay(true);
      component.someOutput.emit('someOutput emitted');
      expect(component.oneWay).toBe('test');
      expect(twoWayChange).toHaveBeenCalledWith(true);
      expect(someOutput).toHaveBeenCalledWith('someOutput emitted');
    });
  });

  describe('with external component', () => {
    const twoWayChange = jest.fn(a => a);
    const someOutput = jest.fn(a => a);

    beforeEach(() => {
      spectator = createDirective(
        `
        <div *rocketReplaceableTemplate="{inputs: {oneWay: {value: oneWay}, twoWay: {value: twoWay, twoWay: true}}, outputs: {twoWayChange: twoWayChange, someOutput: someOutput}, componentKey: 'TestModule.TestComponent'}; let initTemplate = initTemplate">
          <rocket-default-component #defaultComponent="rocketDefaultComponent"></rocket-default-component>
        </div>
        `,
        { hostProps: { oneWay: { label: 'Test' }, twoWay: false, twoWayChange, someOutput } },
      );
      selectResponse.next({ component: ExternalComponent, key: 'TestModule.TestComponent' });
    });

    afterEach(() => twoWayChange.mockClear());

    it('should display the external component', () => {
      expect(spectator.query('p')).toHaveText('external');
    });

    it('should be injected the data object', () => {
      const externalComponent = spectator.query(ExternalComponent);
      expect(externalComponent.data).toEqual({
        componentKey: 'TestModule.TestComponent',
        inputs: { oneWay: { label: 'Test' }, twoWay: false },
        outputs: { someOutput, twoWayChange },
      });
    });

    it('should be worked all data properties', () => {
      const externalComponent = spectator.query(ExternalComponent);
      spectator.setHostInput({ oneWay: 'test' });
      externalComponent.data.inputs.twoWay = true;
      externalComponent.data.outputs.someOutput('someOutput emitted');
      expect(externalComponent.data.inputs.oneWay).toBe('test');
      expect(twoWayChange).toHaveBeenCalledWith(true);
      expect(someOutput).toHaveBeenCalledWith('someOutput emitted');

      spectator.setHostInput({ twoWay: 'twoWay test' });
      expect(externalComponent.data.inputs.twoWay).toBe('twoWay test');
    });

    it('should be worked correctly the default component when the external component has been removed from store', () => {
      expect(spectator.query('p')).toHaveText('external');
      const externalComponent = spectator.query(ExternalComponent);
      spectator.setHostInput({ oneWay: 'test' });
      externalComponent.data.inputs.twoWay = true;
      selectResponse.next({ component: null, key: 'TestModule.TestComponent' });
      spectator.detectChanges();
      const component = spectator.query(DefaultComponent);
      spectator.directive.context.initTemplate(component);
      expect(spectator.query('rocket-default-component')).toBeTruthy();

      expect(component.oneWay).toEqual('test');
      expect(component.twoWay).toEqual(true);
    });

    it('should reset default component subscriptions', () => {
      selectResponse.next({ component: null, key: 'TestModule.TestComponent' });
      const component = spectator.query(DefaultComponent);
      spectator.directive.context.initTemplate(component);
      spectator.detectChanges();
      const unsubscribe = jest.fn(() => {});
      spectator.directive.defaultComponentSubscriptions.twoWayChange.unsubscribe = unsubscribe;

      selectResponse.next({ component: ExternalComponent, key: 'TestModule.TestComponent' });
      expect(unsubscribe).toHaveBeenCalled();
    });
  });
});
