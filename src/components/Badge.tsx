import type { Badge as BadgeType } from '../data/profile'
import { Infinity, Gem, Crown } from 'lucide-react'

const iconMap: Record<string, typeof Infinity> = {
  infinity: Infinity,
  gem: Gem,
  crown: Crown,
}

interface BadgeProps extends BadgeType {}

export function Badge({ color, icon }: BadgeProps) {
  const Icon = iconMap[icon] ?? Infinity
  return (
    <div
      className={`w-14 h-14 rounded-full flex items-center justify-center text-white flex-shrink-0 ${color}`}
    >
      <Icon size={24} strokeWidth={2} />
    </div>
  )
}
