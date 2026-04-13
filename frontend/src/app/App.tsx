import { Route, Routes } from 'react-router-dom'
import { AuthProvider } from '../features/auth/AuthContext'
import { ProtectedRoute } from '../features/auth/ProtectedRoute'
import { RoleRoute } from '../features/auth/RoleRoute'
import { AdminMasterPage } from '../pages/AdminMasterPage'
import { DashboardPage } from '../pages/DashboardPage'
import { LoginPage } from '../pages/LoginPage'

export function App() {
  return (
    <AuthProvider>
      <Routes>
               <Route path="/login" element={<LoginPage />} />
        <Route element={<ProtectedRoute />}>
          <Route index element={<DashboardPage />} />
          <Route element={<RoleRoute roles={['Administrador Master']} />}>
            <Route path="admin" element={<AdminMasterPage />} />
          </Route>
        </Route>
      </Routes>
    </AuthProvider>
  )
}
