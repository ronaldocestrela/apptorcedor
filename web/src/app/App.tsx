import { BrowserRouter } from 'react-router-dom'
import { TenantNotResolvedPage } from '../pages/system/TenantNotResolvedPage'
import { syncTenantFromWindow } from '../shared/tenant'
import { AppRouter } from './router/AppRouter'

export function App() {
  const tenantResult = syncTenantFromWindow()

  if (!tenantResult.ok) {
    return (
      <TenantNotResolvedPage
        hostname={typeof window !== 'undefined' ? window.location.hostname : ''}
        reason={tenantResult.reason}
      />
    )
  }

  return (
    <BrowserRouter>
      <AppRouter />
    </BrowserRouter>
  )
}
