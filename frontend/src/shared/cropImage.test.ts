import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { getCroppedImg } from './cropImage'
import type { Area } from 'react-easy-crop'

// ──────────────────────────────────────────
// Browser API mocks
// ──────────────────────────────────────────

const mockDrawImage = vi.fn()
const mockToBlob = vi.fn()
const mockGetContext = vi.fn()

const mockCreateImageBitmap = vi.fn()

beforeEach(() => {
  // Reset call counts
  mockDrawImage.mockClear()
  mockToBlob.mockClear()
  mockGetContext.mockClear()

  // createImageBitmap mock — returns a simple object (the bitmap handle)
  mockCreateImageBitmap.mockResolvedValue({ width: 200, height: 200 })
  vi.stubGlobal('createImageBitmap', mockCreateImageBitmap)

  // fetch mock — returns a Response whose .blob() resolves to a Blob
  const fakeBlob = new Blob(['fake-image-data'], { type: 'image/jpeg' })
  vi.stubGlobal('fetch', vi.fn().mockResolvedValue({ blob: () => Promise.resolve(fakeBlob) }))

  // Canvas mock
  mockGetContext.mockReturnValue({ drawImage: mockDrawImage })
  mockToBlob.mockImplementation((cb: (b: Blob | null) => void) => {
    cb(new Blob(['cropped'], { type: 'image/jpeg' }))
  })

  const originalCreateElement = document.createElement.bind(document)
  vi.spyOn(document, 'createElement').mockImplementation((tag: string, ...rest) => {
    if (tag === 'canvas') {
      const canvas = originalCreateElement('canvas') as HTMLCanvasElement
      Object.defineProperty(canvas, 'getContext', { value: mockGetContext, writable: true })
      Object.defineProperty(canvas, 'toBlob', { value: mockToBlob, writable: true })
      return canvas
    }
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    return originalCreateElement(tag, ...rest as any)
  })
})

afterEach(() => {
  vi.restoreAllMocks()
  vi.unstubAllGlobals()
})

// ──────────────────────────────────────────
// Tests
// ──────────────────────────────────────────

describe('getCroppedImg', () => {
  const crop: Area = { x: 10, y: 20, width: 100, height: 100 }
  const imageSrc = 'data:image/jpeg;base64,/9j/fake'

  it('returns a Blob when all browser APIs succeed', async () => {
    const blob = await getCroppedImg(imageSrc, crop)
    expect(blob).toBeInstanceOf(Blob)
  })

  it('sets canvas dimensions to the crop area size', async () => {
    await getCroppedImg(imageSrc, crop)
    // The canvas created by getCroppedImg should have width/height matching crop
    // We verify drawImage was called with correct crop coordinates
    expect(mockDrawImage).toHaveBeenCalledOnce()
    const [, sx, sy, sw, sh, dx, dy, dw, dh] = mockDrawImage.mock.calls[0] as number[]
    expect(sx).toBe(crop.x)
    expect(sy).toBe(crop.y)
    expect(sw).toBe(crop.width)
    expect(sh).toBe(crop.height)
    expect(dx).toBe(0)
    expect(dy).toBe(0)
    expect(dw).toBe(crop.width)
    expect(dh).toBe(crop.height)
  })

  it('calls toBlob with image/jpeg and quality 0.9', async () => {
    await getCroppedImg(imageSrc, crop)
    expect(mockToBlob).toHaveBeenCalledWith(expect.any(Function), 'image/jpeg', 0.9)
  })

  it('throws when getContext returns null (canvas unavailable)', async () => {
    mockGetContext.mockReturnValueOnce(null)
    await expect(getCroppedImg(imageSrc, crop)).rejects.toThrow('Canvas 2D context not available')
  })

  it('rejects when toBlob returns null', async () => {
    mockToBlob.mockImplementationOnce((cb: (b: Blob | null) => void) => cb(null))
    await expect(getCroppedImg(imageSrc, crop)).rejects.toThrow()
  })

  it('fetches the imageSrc URL to build the ImageBitmap', async () => {
    await getCroppedImg(imageSrc, crop)
    expect(fetch).toHaveBeenCalledWith(imageSrc)
  })
})
