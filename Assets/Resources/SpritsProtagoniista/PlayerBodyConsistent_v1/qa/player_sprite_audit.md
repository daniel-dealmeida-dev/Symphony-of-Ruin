# Player sprite audit - body consistent v1

Metodo: escala e alinhamento avaliados pela mascara do corpo, excluindo efeitos brilhantes e vermelho dominante da guitarra. A altura total do PNG nao e usada como criterio de escala.
Canvas: 416x288; baseline_y: 260; anchor_x: 208; pivot Unity: (0.5, 0.410526); PPU: 55.

Resumo dos testes:
- idle: frames=8, bottom=[260, 260], anchor=[208, 208], dense_height=[199, 203], body_area=[12855, 12943]
- walk: frames=8, bottom=[260, 260], anchor=[205, 208], dense_height=[188, 207], body_area=[12082, 14054]
- jump: frames=4, bottom=[260, 260], anchor=[206, 207], dense_height=[169, 194], body_area=[12303, 14120]
- fall: frames=4, bottom=[260, 260], anchor=[207, 210], dense_height=[169, 202], body_area=[12303, 13841]
- hit: frames=4, bottom=[260, 260], anchor=[207, 211], dense_height=[191, 213], body_area=[5376, 14412]
- death: frames=8, bottom=[260, 260], anchor=[176, 209], dense_height=[134, 202], body_area=[12923, 13455]
- attack_01: frames=11, bottom=[260, 260], anchor=[207, 209], dense_height=[141, 224], body_area=[8438, 15016]
- attack_02: frames=9, bottom=[260, 260], anchor=[207, 289], dense_height=[130, 208], body_area=[8849, 30287]
- attack_03: frames=11, bottom=[260, 260], anchor=[207, 209], dense_height=[146, 184], body_area=[11656, 14307]
- attack_04: frames=13, bottom=[260, 260], anchor=[188, 209], dense_height=[177, 226], body_area=[8415, 15024]

Falhas bloqueantes: 0
Avisos revisados de pose/ancora: 3
- ('death', 7, 'pose_anchor', -32)
- ('attack_02', 6, 'pose_anchor', 81)
- ('attack_04', 11, 'pose_anchor', -20)

Os avisos de ancora sao deslocamentos naturais de pose em caminhada, dano, morte ou ataque; a linha de apoio do corpo ficou em y=260 em todos os frames.
