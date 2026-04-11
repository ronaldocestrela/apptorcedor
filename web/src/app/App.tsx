import { BrowserRouter } from 'react-router-dom'
import { TenantNotResolvedPage } from '../pages/system/TenantNotResolvedPage'
import { syncTenantFromWindow } from '../shared/tenant'
import { AuthProvider } from './auth/AuthProvider'
import { BackofficeRouter } from './backoffice/BackofficeRouter'
import { AppRouter } from './router/AppRouter'

function isBackofficePath(): boolean {
  if (typeof window === 'undefined') {
    return false
  }
  return window.location.pathname.startsWith('/backoffice')
}

export function App() {
  if (isBackofficePath()) {
    return (
      <BrowserRouter>
        <BackofficeRouter />
      </BrowserRouter>
    )
  }

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
      <AuthProvider>
        <AppRouter />
      </AuthProvider>
    </BrowserRouter>
  )
}
