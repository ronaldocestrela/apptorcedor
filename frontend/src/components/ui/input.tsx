import * as React from 'react'
import { cn } from '../../lib/utils'

const Input = React.forwardRef<HTMLInputElement, React.ComponentProps<'input'>>(({ className, ...props }, ref) => {
  return (
    <input
      className={cn(
        'flex h-10 w-full rounded-md border border-[rgba(255,255,255,0.16)] bg-[rgba(255,255,255,0.04)] px-3 py-2 text-sm text-[#f5f7fa] placeholder:text-[#8b97aa] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[#8cd392] disabled:cursor-not-allowed disabled:opacity-50',
        className,
      )}
      ref={ref}
      {...props}
    />
  )
})
Input.displayName = 'Input'

export { Input }
