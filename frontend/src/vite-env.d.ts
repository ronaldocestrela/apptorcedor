/// <reference types="vite/client" />

interface ImportMetaEnv {
  readonly VITE_API_URL?: string
  /** OAuth2.0 Web Client ID for Google Sign-In (GIS). */
  readonly VITE_GOOGLE_CLIENT_ID?: string
  /** Sigla do clube na casa exibida nos cards de jogos (ex.: FFC). */
  readonly VITE_CLUB_SHORT_NAME?: string
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}
