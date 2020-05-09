import { CoreModule } from '@rocket/ng.core';
import { ThemeSharedModule } from '@rocket/ng.theme.shared';
import { NgModule, Provider } from '@angular/core';
import { NgbDropdownModule } from '@ng-bootstrap/ng-bootstrap';
import { NgxsModule } from '@ngxs/store';
import { TenantsComponent } from './components/tenants/tenants.component';
import { TenantManagementState } from './states/tenant-management.state';
import { TenantManagementRoutingModule } from './tenant-management-routing.module';
import { FeatureManagementModule } from '@rocket/ng.feature-management';
import { NgxValidateCoreModule } from '@ngx-validate/core';

@NgModule({
  declarations: [TenantsComponent],
  imports: [
    TenantManagementRoutingModule,
    NgxsModule.forFeature([TenantManagementState]),
    NgxValidateCoreModule,
    CoreModule,
    ThemeSharedModule,
    NgbDropdownModule,
    FeatureManagementModule,
  ],
})
export class TenantManagementModule {}