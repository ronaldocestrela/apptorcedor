export interface Product {
  id: number;
  name: string;
  price: number;
  image: string;
}

export const products: Product[] = [
  {
    id: 1,
    name: 'Camisa Oficial',
    price: 100,
    image: 'https://picsum.photos/seed/prod1/300/300',
  },
  {
    id: 2,
    name: 'Camisa Treino',
    price: 100,
    image: 'https://picsum.photos/seed/prod2/300/300',
  },
  {
    id: 3,
    name: 'Short Oficial',
    price: 80,
    image: 'https://picsum.photos/seed/prod3/300/300',
  },
  {
    id: 4,
    name: 'Boné FFC',
    price: 50,
    image: 'https://picsum.photos/seed/prod4/300/300',
  },
];
