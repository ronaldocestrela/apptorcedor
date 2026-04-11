import { Navigate, Route, Routes } from 'react-router-dom'
import { BackofficeDashboardPage } from '../../pages/backoffice/BackofficeDashboardPage'
import { BackofficeLoginPage } from '../../pages/backoffice/BackofficeLoginPage'
import { SaaSPlansListPage } from '../../pages/backoffice/SaaSPlansListPage'
import { TenantDetailPage } from '../../pages/backoffice/TenantDetailPage'
import { TenantPlansPage } from '../../pages/backoffice/TenantPlansPage'
import { TenantsListPage } from '../../pages/backoffice/TenantsListPage'
import { BackofficeShell } from './BackofficeShell'
import { RequireBackofficeAuth } from './RequireBackofficeAuth'

export function BackofficeRouter() {
  return (
    <Routes>
      <Route path="/backoffice/login" element={<BackofficeLoginPage />} />
      <Route element={<RequireBackofficeAuth />}>
        <Route path="/backoffice" element={<BackofficeShell />}>
          <Route index element={<BackofficeDashboardPage />} />
          <Route path="tenants" element={<TenantsListPage />} />
          <Route path="tenants/:id" element={<TenantDetailPage />} />
          <Route path="plans" element={<SaaSPlansListPage />} />
          <Route path="tenant-plans" element={<TenantPlansPage />} />
          <Route path="*" element={<Navigate to="/backoffice" replace />} />
        </Route>
      </Route>
    </Routes>
  )
}
