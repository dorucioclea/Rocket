import { takeUntilDestroy } from '@aiwins/ng.core';
import {
  Component,
  ContentChild,
  ElementRef,
  EventEmitter,
  Input,
  OnDestroy,
  Output,
  Renderer2,
  TemplateRef,
  ViewChild,
  ViewChildren,
} from '@angular/core';
import { fromEvent, Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged, filter, takeUntil } from 'rxjs/operators';
import { fadeAnimation } from '../../animations/modal.animations';
import { Confirmation } from '../../models/confirmation';
import { ConfirmationService } from '../../services/confirmation.service';
import { ModalService } from '../../services/modal.service';
import { ButtonComponent } from '../button/button.component';

export type ModalSize = 'sm' | 'md' | 'lg' | 'xl';

@Component({
  selector: 'rocket-modal',
  templateUrl: './modal.component.html',
  animations: [fadeAnimation],
  styleUrls: ['./modal.component.scss'],
  providers: [ModalService],
})
export class ModalComponent implements OnDestroy {
  @Input()
  get visible(): boolean {
    return this._visible;
  }
  set visible(value: boolean) {
    if (typeof value !== 'boolean') return;
    this.toggle$.next(value);
  }

  @Input()
  get busy(): boolean {
    return this._busy;
  }
  set busy(value: boolean) {
    if (this.rocketSubmit && this.rocketSubmit instanceof ButtonComponent) {
      this.rocketSubmit.loading = value;
    }

    this._busy = value;
  }

  @Input() centered = false;

  @Input() modalClass = '';

  @Input() size: ModalSize = 'lg';

  @ContentChild(ButtonComponent, { static: false, read: ButtonComponent })
  rocketSubmit: ButtonComponent;

  @ContentChild('rocketHeader', { static: false }) rocketHeader: TemplateRef<any>;

  @ContentChild('rocketBody', { static: false }) rocketBody: TemplateRef<any>;

  @ContentChild('rocketFooter', { static: false }) rocketFooter: TemplateRef<any>;

  @ContentChild('rocketClose', { static: false, read: ElementRef })
  rocketClose: ElementRef<any>;

  @ViewChild('template', { static: false }) template: TemplateRef<any>;

  @ViewChild('rocketModalContent', { static: false }) modalContent: ElementRef;

  @ViewChildren('rocket-button') rocketButtons;

  @Output() readonly visibleChange = new EventEmitter<boolean>();

  @Output() readonly init = new EventEmitter<void>();

  @Output() readonly appear = new EventEmitter();

  @Output() readonly disappear = new EventEmitter();

  _visible = false;

  _busy = false;

  isModalOpen = false;

  isConfirmationOpen = false;

  destroy$ = new Subject<void>();

  private toggle$ = new Subject<boolean>();

  get isFormDirty(): boolean {
    return Boolean(document.querySelector('.modal-dialog .ng-dirty'));
  }

  constructor(
    private renderer: Renderer2,
    private confirmationService: ConfirmationService,
    private modalService: ModalService,
  ) {
    this.initToggleStream();
  }

  private initToggleStream() {
    this.toggle$
      .pipe(takeUntilDestroy(this), debounceTime(0), distinctUntilChanged())
      .subscribe(value => this.toggle(value));
  }

  private toggle(value: boolean) {
    this.isModalOpen = value;
    this._visible = value;
    this.visibleChange.emit(value);

    if (value) {
      this.modalService.renderTemplate(this.template);
      setTimeout(() => this.listen(), 0);
      this.renderer.addClass(document.body, 'modal-open');
      this.appear.emit();
    } else {
      this.modalService.clearModal();
      this.renderer.removeClass(document.body, 'modal-open');
      this.disappear.emit();
      this.destroy$.next();
    }
  }

  ngOnDestroy(): void {
    this.destroy$.next();
  }

  close() {
    if (this.busy) return;

    if (this.isFormDirty) {
      if (this.isConfirmationOpen) return;

      this.isConfirmationOpen = true;
      this.confirmationService
        .warn(
          'RocketAccount::AreYouSureYouWantToCancelEditingWarningMessage',
          'RocketAccount::AreYouSure',
        )
        .subscribe((status: Confirmation.Status) => {
          this.isConfirmationOpen = false;
          if (status === Confirmation.Status.confirm) {
            this.visible = false;
          }
        });
    } else {
      this.visible = false;
    }
  }

  listen() {
    fromEvent(document, 'keyup')
      .pipe(
        takeUntil(this.destroy$),
        debounceTime(150),
        filter((key: KeyboardEvent) => key && key.key === 'Escape'),
      )
      .subscribe(() => this.close());

    fromEvent(window, 'beforeunload')
      .pipe(takeUntil(this.destroy$))
      .subscribe(event => {
        if (this.isFormDirty) {
          event.returnValue = true;
        } else {
          event.returnValue = false;
          delete event.returnValue;
        }
      });

    setTimeout(() => {
      if (!this.rocketClose) return;
      fromEvent(this.rocketClose.nativeElement, 'click')
        .pipe(
          takeUntil(this.destroy$),
          filter(() => !!this.modalContent),
        )
        .subscribe(() => this.close());
    }, 0);

    this.init.emit();
  }
}
