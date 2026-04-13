/** Minimal typing for Google Identity Services (GIS). */
declare global {
  interface Window {
    google?: {
      accounts: {
        id: {
          initialize: (config: {
            client_id: string
            callback: (resp: { credential: string }) => void
          }) => void
          renderButton: (
            parent: HTMLElement,
            options: { type?: string; theme?: string; size?: string; width?: string | number },
          ) => void
        }
      }
    }
  }
}

export function loadGoogleScript(): Promise<void> {
  if (window.google?.accounts?.id)
    return Promise.resolve()

  return new Promise((resolve, reject) => {
    const existing = document.querySelector('script[data-google-gsi]')
    if (existing) {
      existing.addEventListener('load', () => resolve())
      existing.addEventListener('error', () => reject(new Error('Falha ao carregar Google')))
      return
    }

    const s = document.createElement('script')
    s.src = 'https://accounts.google.com/gsi/client'
    s.async = true
    s.defer = true
    s.dataset.googleGsi = 'true'
    s.onload = () => resolve()
    s.onerror = () => reject(new Error('Falha ao carregar Google'))
    document.head.appendChild(s)
  })
}
