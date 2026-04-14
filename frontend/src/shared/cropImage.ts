import type { Area } from 'react-easy-crop'

export async function getCroppedImg(imageSrc: string, pixelCrop: Area): Promise<Blob> {
  const image = await createImageBitmap(await (await fetch(imageSrc)).blob())

  const canvas = document.createElement('canvas')
  canvas.width = pixelCrop.width
  canvas.height = pixelCrop.height

  const ctx = canvas.getContext('2d')
  if (!ctx)
    throw new Error('Canvas 2D context not available')

  ctx.drawImage(
    image,
    pixelCrop.x,
    pixelCrop.y,
    pixelCrop.width,
    pixelCrop.height,
    0,
    0,
    pixelCrop.width,
    pixelCrop.height,
  )

  return new Promise((resolve, reject) => {
    canvas.toBlob(
      (blob) => {
        if (blob)
          resolve(blob)
        else
          reject(new Error('Canvas toBlob failed'))
      },
      'image/jpeg',
      0.9,
    )
  })
}
