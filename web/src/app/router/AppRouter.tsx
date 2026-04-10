import { Navigate, Route, Routes } from 'react-router-dom'
import { AdminHomePage } from '../../pages/admin/AdminHomePage'
import { MemberHomePage } from '../../pages/member/MemberHomePage'
import { AppShell } from './AppShell'

export function AppRouter() {
  return (
    <Routes>
      <Route element={<AppShell />}>
        <Route index element={<Navigate to="/member" replace />} />
        <Route path="admin" element={<AdminHomePage />} />
        <Route path="member" element={<MemberHomePage />} />
      </Route>
      <Route path="*" element={<Navigate to="/member" replace />} />
    </Routes>
  )
}
