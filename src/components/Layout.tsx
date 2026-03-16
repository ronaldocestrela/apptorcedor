import { Outlet, useLocation } from "react-router-dom";
import { Header } from "./Header";
import { BottomNav } from "./BottomNav";

function getPageTitle(pathname: string): string {
  if (pathname.startsWith("/noticias")) return "NOTÍCIAS";
  if (pathname === "/loja") return "LOJA";
  if (pathname === "/perfil") return "PERFIL";
  return "NOTÍCIAS";
}

export function Layout() {
  const location = useLocation();
  const title = getPageTitle(location.pathname);
  return (
    <div className="relative min-h-full flex flex-col flex-1">
      <Header title={title} />
      <main className="flex-1 pb-20 overflow-auto">
        <Outlet />
      </main>
      <BottomNav />
    </div>
  );
}
