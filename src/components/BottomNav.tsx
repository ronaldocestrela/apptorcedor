import { Link, useLocation } from "react-router-dom";
import { Store, User } from "lucide-react";

export function BottomNav() {
  const location = useLocation();

  const isActive = (path: string) => {
    if (path === "/") {
      return location.pathname === "/" || location.pathname.startsWith("/noticias");
    }
    return location.pathname.startsWith(path);
  };

  return (
    <nav className="absolute bottom-0 left-0 right-0 bg-ffc-green h-16 flex items-center justify-around z-10">
      <Link
        to="/loja"
        className="flex flex-col items-center justify-center flex-1 h-full text-white hover:bg-ffc-green-light transition-colors"
      >
        <Store className="w-6 h-6" />
        {isActive("/loja") && (
          <span className="w-8 h-0.5 bg-white rounded mt-1" />
        )}
      </Link>

      <Link
        to="/"
        className="flex flex-col items-center justify-center flex-1 h-full text-white hover:bg-ffc-green-light transition-colors"
      >
        <img
          src="/ESCUDO_FFC_PNG.png"
          alt="Home"
          className="w-8 h-8 object-contain"
        />
        {isActive("/") && (
          <span className="w-8 h-0.5 bg-white rounded mt-1" />
        )}
      </Link>

      <Link
        to="/perfil"
        className="flex flex-col items-center justify-center flex-1 h-full text-white hover:bg-ffc-green-light transition-colors"
      >
        <User className="w-6 h-6" />
        {isActive("/perfil") && (
          <span className="w-8 h-0.5 bg-white rounded mt-1" />
        )}
      </Link>
    </nav>
  );
}

