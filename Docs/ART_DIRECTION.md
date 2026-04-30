# Direcao de Arte - Symphony of Ruin

## Norte visual

O jogo deve manter uma fantasia sombria em pixel art, com leitura clara de silhueta e contraste forte entre personagem, inimigos e fundo. A paleta principal deve ficar em azuis frios, roxos escuros, cinzas e pretos azulados, com acentos quentes pontuais em fogo, guitarra, dano, moedas e feedback de impacto.

## Regras de sprites

- Sprites de gameplay devem usar `Filter Mode: Point`, mipmaps desligados e compressao desligada.
- Evitar blur, textura fotografica e recortes suavizados nos elementos jogaveis.
- Manter contorno escuro legivel em personagens, inimigos, coletaveis e objetos interativos.
- Fundos podem ser mais suaves e menos contrastados para nao competir com o gameplay.
- Nao alterar pivots, retangulos de slicing, PPU ou GUIDs sem revisar cenas, prefabs e animacoes.
- Assets autorais de foreground devem usar paleta mais limitada, borda interna escura e highlights discretos no topo para combinar melhor com o pacote pixel art.
- Coletaveis devem usar acentos quentes para leitura imediata sobre os fundos frios do jogo.

## Escala e leitura

- Orfeu e inimigos precisam ter silhuetas reconheciveis em tamanho real de jogo.
- Elementos de primeiro plano devem ter contraste maior que fundos.
- VFX de impacto devem ser curtos, claros e mais brilhantes que o sprite base.
- Coletaveis devem ter borda externa suficiente para aparecer sobre fundo claro e escuro.

## Animacao

- Caminhada/corrida deve fechar ciclo rapido, entre 0.55s e 0.8s.
- Ataque deve ter antecipacao curta, frame de impacto evidente e recuperacao rapida.
- Morte deve permanecer tempo suficiente para leitura antes do objeto sumir.
- Animacoes de idle podem ser discretas; evitar movimento excessivo que pareca tremor.

## Ajustes aplicados

- Orfeu, OzzyBat, moeda e lixeira receberam reforco de contraste, leitura de silhueta e importacao sem compressao.
- Blocos, gramas, matos, arvore, montanha, lua e nuvens autorais receberam polimento de paleta e nitidez mantendo dimensoes originais.
- Fundos autorais receberam color grading frio e reducao sutil de competicao visual com a camada de gameplay.
- As spritesheets e nomes atuais foram preservados para nao quebrar referencias em animacoes, controllers, cenas e prefabs.

## Organizacao recomendada

Em uma limpeza futura, migrar gradualmente os assets autorais para:

- `Assets/Game/Art/Characters/Player`
- `Assets/Game/Art/Characters/Enemies`
- `Assets/Game/Art/Environment`
- `Assets/Game/Art/VFX`
- `Assets/Game/Art/UI`

Ao mover, preservar `.meta` para manter GUIDs e referencias Unity.
