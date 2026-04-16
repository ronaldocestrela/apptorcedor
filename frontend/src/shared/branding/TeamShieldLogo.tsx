import { useEffect, useState } from 'react'
import { resolvePublicAssetUrl } from '../../features/account/accountApi'
import { getPublicBranding } from './brandingApi'
import { getTeamShieldPlaceholderDataUrl } from './teamShieldPlaceholder'

type TeamShieldLogoProps = {
  className?: string
  alt?: string
  width?: number
  height?: number
}

export function TeamShieldLogo({
  className,
  alt = 'Escudo do clube',
  width,
  height,
}: TeamShieldLogoProps) {
  const [src, setSrc] = useState(() => getTeamShieldPlaceholderDataUrl())

  useEffect(() => {
    let cancelled = false
    void (async () => {
      try {
        const b = await getPublicBranding()
        const resolved = resolvePublicAssetUrl(b.teamShieldUrl ?? undefined)
        if (!cancelled && resolved)
          setSrc(resolved)
      }
      catch {
        /* keep placeholder */
      }
    })()
    return () => {
      cancelled = true
    }
  }, [])

  return (
    <img
      className={className}
      src={src}
      alt={alt}
      width={width}
      height={height}
      decoding="async"
    />
  )
}
