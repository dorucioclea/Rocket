import { ROCKET, SetTenant, SessionState, GetAppConfiguration } from '@rocket/ng.core';
import { ToasterService } from '@rocket/ng.theme.shared';
import { Component, OnInit } from '@angular/core';
import { Store } from '@ngxs/store';
import { throwError } from 'rxjs';
import { catchError, take, finalize, switchMap } from 'rxjs/operators';
import snq from 'snq';
import { AccountService } from '../../services/account.service';
import { Account } from '../../models/account';

@Component({
  selector: 'rocket-tenant-box',
  templateUrl: './tenant-box.component.html',
})
export class TenantBoxComponent
  implements OnInit, Account.TenantBoxComponentInputs, Account.TenantBoxComponentOutputs {
  tenant = {} as ROCKET.BasicItem;

  tenantName: string;

  isModalVisible: boolean;

  inProgress: boolean;

  constructor(
    private store: Store,
    private toasterService: ToasterService,
    private accountService: AccountService,
  ) {}

  ngOnInit() {
    this.tenant = this.store.selectSnapshot(SessionState.getTenant) || ({} as ROCKET.BasicItem);
    this.tenantName = this.tenant.name || '';
  }

  onSwitch() {
    this.isModalVisible = true;
  }

  save() {
    if (this.tenant.name && !this.inProgress) {
      this.inProgress = true;
      this.accountService
        .findTenant(this.tenant.name)
        .pipe(
          finalize(() => (this.inProgress = false)),
          take(1),
          catchError(err => {
            this.toasterService.error(
              snq(() => err.error.error_description, 'RocketUi::DefaultErrorMessage'),
              'RocketUi::Error',
            );
            return throwError(err);
          }),
          switchMap(({ success, tenantId }) => {
            if (success) {
              this.tenant = {
                id: tenantId,
                name: this.tenant.name,
              };
              this.tenantName = this.tenant.name;
              this.isModalVisible = false;
            } else {
              this.toasterService.error(
                'RocketUiMultiTenancy::GivenTenantIsNotAvailable',
                'RocketUi::Error',
                {
                  messageLocalizationParams: [this.tenant.name],
                },
              );
              this.tenant = {} as ROCKET.BasicItem;
              this.tenantName = '';
            }
            this.store.dispatch(new SetTenant(success ? this.tenant : null));
            return this.store.dispatch(new GetAppConfiguration());
          }),
        )
        .subscribe();
    } else {
      this.store.dispatch([new SetTenant(null), new GetAppConfiguration()]);
      this.tenantName = null;
      this.isModalVisible = false;
    }
  }
}
