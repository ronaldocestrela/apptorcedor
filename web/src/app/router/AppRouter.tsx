import { Navigate, Route, Routes } from 'react-router-dom'
import { AdminHomePage } from '../../pages/admin/AdminHomePage'
import { LoginPage } from '../../pages/auth/LoginPage'
import { RegisterPage } from '../../pages/auth/RegisterPage'
import { MemberHomePage } from '../../pages/member/MemberHomePage'
import { AppShell } from './AppShell'
import { RequireAuth } from './RequireAuth'

export function AppRouter() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<RegisterPage />} />
      <Route element={<RequireAuth />}>
        <Route element={<AppShell />}>
          <Route index element={<Navigate to="/member" replace />} />
          <Route path="admin" element={<AdminHomePage />} />
          <Route path="member" element={<MemberHomePage />} />
        </Route>
      </Route>
      <Route path="*" element={<Navigate to="/member" replace />} />
    </Routes>
  )
}
