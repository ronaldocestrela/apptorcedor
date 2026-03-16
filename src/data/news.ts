export interface NewsItem {
  id: number;
  title: string;
  summary: string;
  body: string;
  image: string;
}

export const newsList: NewsItem[] = [
  {
    id: 1,
    title: 'FEIRA FUTEBOL CLUBE',
    summary:
      'O Feira Futebol Clube, novo projeto esportivo profissional de Feira de Santana (BA) fundado em 2025 e registrado no BID da CBF, já se prepara para fazer sua estreia oficial nas competições em 2026 ao disputar a Série B do Campeonato Baiano.',
    body: 'O Feira Futebol Clube, novo projeto esportivo profissional de Feira de Santana (BA) fundado em 2025 e registrado no BID da CBF, já se prepara para fazer sua estreia oficial nas competições em 2026 ao disputar a Série B do Campeonato Baiano.\n\nA equipe, que contratou nomes como Thiago Galhardo e Lucaneta, ganhou um certo público com essas contratações. Com isso, fica a pergunta: quando o time estreia no ano? O Sporting News mostra.',
    image: 'https://picsum.photos/seed/ffc1/400/300',
  },
  {
    id: 2,
    title: 'NOVA CONTRATAÇÃO',
    summary:
      'O Feira Futebol Clube, novo projeto esportivo profissional de Feira de Santana (BA) fundado em 2025 e registrado no BID da CBF, já se prepara para fazer sua estreia oficial nas competições em 2026 ao disputar a Série B do Campeonato Baiano.',
    body: 'O Feira Futebol Clube anunciou mais uma contratação de peso para a temporada 2026. O jogador reforça o meio-campo e promete muita garra nas competições.',
    image: 'https://picsum.photos/seed/ffc2/400/300',
  },
  {
    id: 3,
    title: 'REFORÇO NO CLUBE',
    summary:
      'O Feira Futebol Clube, novo projeto esportivo profissional de Feira de Santana (BA) fundado em 2025 e registrado no BID da CBF, já se prepara para fazer sua estreia oficial nas competições em 2026 ao disputar a Série B do Campeonato Baiano.',
    body: 'Reforços importantes chegam ao Feira Futebol Clube para fortalecer o elenco na disputa da Série B do Campeonato Baiano.',
    image: 'https://picsum.photos/seed/ffc3/400/300',
  },
  {
    id: 4,
    title: 'COLETIVA 11/03',
    summary:
      'O Feira Futebol Clube, novo projeto esportivo profissional de Feira de Santana (BA) fundado em 2025 e registrado no BID da CBF, já se prepara para fazer sua estreia oficial nas competições em 2026 ao disputar a Série B do Campeonato Baiano.',
    body: 'Confira os principais momentos da coletiva de imprensa realizada em 11/03, com declarações do técnico e jogadores.',
    image: 'https://picsum.photos/seed/ffc4/400/300',
  },
];
