export interface Product {
  id: number;
  name: string;
  price: number;
  image: string;
}

export const products: Product[] = [
  {
    id: 1,
    name: 'Mochila FFC',
    price: 100,
    image: 'loja01.jpeg',
  },
  {
    id: 2,
    name: 'Kit Torcedor',
    price: 100,
    image: 'loja02.jpeg',
  },
  {
    id: 3,
    name: 'Chaveiro FFC',
    price: 80,
    image: 'loja03.jpeg',
  },
  {
    id: 4,
    name: 'Camiseta FFC',
    price: 50,
    image: 'loja04.jpeg',
  },
];
