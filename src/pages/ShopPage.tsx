import { ProductCard } from '../components/ProductCard'
import { products } from '../data/products'

export function ShopPage() {
  return (
    <div className="flex flex-col flex-1 pb-20">
      <div className="grid grid-cols-2 gap-4 px-4 py-4">
        {products.map((product) => (
          <ProductCard key={product.id} product={product} />
        ))}
      </div>
    </div>
  )
}
