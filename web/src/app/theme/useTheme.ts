import { useCallback, useEffect, useState } from 'react'

export const THEME_STORAGE_KEY = 'theme'

export type Theme = 'light' | 'dark'

function readInitialTheme(): Theme {
  const fromDom = document.documentElement.dataset.theme
  if (fromDom === 'dark' || fromDom === 'light') {
    return fromDom
  }
  const stored = localStorage.getItem(THEME_STORAGE_KEY)
  if (stored === 'dark' || stored === 'light') {
    return stored
  }
  return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light'
}

export function applyTheme(theme: Theme) {
  document.documentElement.dataset.theme = theme
  localStorage.setItem(THEME_STORAGE_KEY, theme)
}

/**
 * Theme synced with `data-theme` on `<html>` and `localStorage` (`theme` key).
 * Inline script in `index.html` sets initial theme before React mounts.
 */
export function useTheme() {
  const [theme, setThemeState] = useState<Theme>(readInitialTheme)

  useEffect(() => {
    applyTheme(theme)
  }, [theme])

  useEffect(() => {
    function onStorage(e: StorageEvent) {
      if (e.key === THEME_STORAGE_KEY && (e.newValue === 'dark' || e.newValue === 'light')) {
        document.documentElement.dataset.theme = e.newValue
        setThemeState(e.newValue)
      }
    }
    window.addEventListener('storage', onStorage)
    return () => window.removeEventListener('storage', onStorage)
  }, [])

  const setTheme = useCallback((next: Theme) => {
    setThemeState(next)
  }, [])

  const toggleTheme = useCallback(() => {
    setThemeState((t) => (t === 'dark' ? 'light' : 'dark'))
  }, [])

  return { theme, setTheme, toggleTheme, isDark: theme === 'dark' }
}
