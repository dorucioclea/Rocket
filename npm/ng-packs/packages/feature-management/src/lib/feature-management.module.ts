import { CoreModule } from '@aiwins/ng.core';
import { ThemeSharedModule } from '@aiwins/ng.theme.shared';
import { NgModule } from '@angular/core';
import { FeatureManagementComponent } from './components/feature-management/feature-management.component';
import { NgxsModule } from '@ngxs/store';
import { FeatureManagementState } from './states/feature-management.state';

@NgModule({
  declarations: [FeatureManagementComponent],
  imports: [CoreModule, ThemeSharedModule, NgxsModule.forFeature([FeatureManagementState])],
  exports: [FeatureManagementComponent],
})
export class FeatureManagementModule {}
