import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { Layout } from './components/Layout'
import { NewsListPage } from './pages/NewsListPage'
import { NewsDetailPage } from './pages/NewsDetailPage'
import { ShopPage } from './pages/ShopPage'
import { ProfilePage } from './pages/ProfilePage'

function App() {
  return (
    <BrowserRouter>
      <div className="min-h-screen flex justify-center bg-gray-700 py-4 px-2">
        <div className="w-full max-w-[430px] min-h-[calc(100vh-2rem)] bg-ffc-bg rounded-2xl shadow-2xl overflow-hidden flex flex-col">
          <Routes>
            <Route path="/" element={<Layout />}>
              <Route index element={<NewsListPage />} />
              <Route path="noticias/:id" element={<NewsDetailPage />} />
              <Route path="loja" element={<ShopPage />} />
              <Route path="perfil" element={<ProfilePage />} />
              <Route path="*" element={<Navigate to="/" replace />} />
            </Route>
          </Routes>
        </div>
      </div>
    </BrowserRouter>
  )
}

export default App
