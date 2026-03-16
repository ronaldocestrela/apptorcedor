interface HeaderProps {
  title: string;
}

export function Header({ title }: HeaderProps) {
  return (
    <header className="bg-white py-4 px-4">
      <div className="flex flex-col items-center gap-2">
        <img
          src="/ESCUDO_FFC_VERDE.png"
          alt="FFC"
          className="h-12 w-12 object-contain"
        />
        <h1 className="text-ffc-green font-bold text-lg uppercase tracking-wide">
          {title}
        </h1>
      </div>
    </header>
  );
}
