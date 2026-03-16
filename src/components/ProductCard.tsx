import type { Product } from '../data/products'

interface ProductCardProps {
  product: Product
}

export function ProductCard({ product }: ProductCardProps) {
  return (
    <article className="flex flex-col bg-white rounded-lg shadow-sm overflow-hidden transition-shadow hover:shadow-md">
      <div className="aspect-square bg-gray-900">
        <img
          src={product.image}
          alt={product.name}
          className="w-full h-full object-cover"
        />
      </div>
      <div className="p-3 flex flex-col gap-2">
        <p className="font-bold text-black text-lg uppercase">{product.name}</p>
        <p className="font-bold text-black text-lg">
          R$ {product.price.toFixed(2).replace('.', ',')}
        </p>
        <button className="w-full py-2 bg-ffc-green text-white font-bold text-sm uppercase rounded-lg hover:bg-ffc-green-light transition-colors active:scale-[0.98]">
          ADICIONAR AO CARRINHO
        </button>
      </div>
    </article>
  )
}
