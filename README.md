# ABISMO / ABYSS

Dungeon crawler infinito por turnos em Godot 4.7.2 .NET e C#. Menus, retratos, mapa e efeitos são desenhados exclusivamente com caracteres ASCII. As ilustrações detalhadas foram convertidas de imagens originais em grades de texto. A tocha decorativa foi removida do menu inicial.

## Jogar

Execute `./jogar.sh`. Requer SDK .NET 10 e Godot .NET. O iniciador compila, importa os recursos e abre o jogo. A edição .NET está em `/home/gabriel/Documents/Godot/Godot_v4.7.2-stable_mono_linux_x86_64/`. Para editar, abra `project.godot` nessa edição e pressione F5.

O menu oferece **Iniciar jogo**, **Idioma** e **Sair do jogo**. Português e English estão disponíveis. A preferência fica salva em `user://settings.cfg` e pode ser alterada pela pausa. A partida não possui salvamento; voltar ao menu encerra a expedição.

## Controles e HUD

| Tecla | Ação |
|---|---|
| Setas / W S + Enter | Navegar e confirmar nos menus |
| 1–4 / A D | Escolher classe |
| WASD / setas | Mover e atacar ao esbarrar em inimigos |
| F + direção | Ataque básico gratuito com cajado (mago) ou arco (arqueiro) |
| I | Abrir inventário |
| Q | Habilidade da classe |
| P | Beber poção de cura 2d10 |
| Espaço | Esperar e recuperar 1 energia; inimigos também agem |
| E | Descer quando estiver em `>`; chefes bloqueiam a passagem |
| Tab | Alternar o alvo entre inimigos visíveis, sem gastar turno |
| Aba Ajuda na pausa | Compêndio de lugares, criaturas, vocações e provisões |
| Esc | Cancelar mira / fechar ajuda / pausar |
| F11 | Tela cheia |

A HUD mostra os retratos, vida e energia do herói, nível, XP, poções e ouro; o inimigo mostra apenas vida. O painel abaixo do mapa reúne habilidades, custos e dados de dano. Uma moldura abaixo da vida do inimigo mostra os dois registros mais recentes do diário e das rolagens, com textos longos abreviados; o histórico completo permanece na pausa.

**Esc abre a ficha de pausa**, sem avançar turnos. Use **1–5, A/D, setas laterais ou Tab** para alternar:

- **Personagem:** atributos e modificadores, recursos, defesas, proficiência, ataques, habilidades e cura.
- **Diário e rolagens:** histórico completo da expedição, mais recente primeiro. Cima/baixo muda a página; Home/End vai ao início/fim. Os registros acompanham o idioma escolhido.
- **Inventário (aba 2 / I):** equipamento, requisitos, raridades, valores e consumíveis. Enter equipa/remove/bebe e gasta um turno.
- **Configurações:** idioma e voltar ao menu principal. Cima/baixo seleciona e Enter confirma. Esc retoma a partida.

O histórico é mantido durante a expedição e reiniciado ao começar outra partida.

TAB escolhe o alvo das habilidades de arqueiro e ladino quando ele está ao alcance. Guerreiro e mago atingem todos os inimigos visíveis dentro da área. Disparos F seguem a direção escolhida.

## Combate de dados

Um ataque rola **d20 + modificador de atributo + proficiência** contra a defesa única do alvo. O modificador é `arredondar para baixo((atributo − 10) / 2)`. Exemplo: atributo 15 fornece +2; atributo 8 fornece −1.

- **1 natural:** falha automática, mesmo com bônus alto.
- **20 natural:** acerto crítico automático. Dobra a quantidade de dados de dano, sem dobrar o bônus fixo.
- **Defesa única:** classe de armadura da peça equipada + modificador de Constituição. Sem armadura, a CA é 10. Ataques físicos e mágicos usam essa mesma defesa.
- **Q:** recebe +2 no teste de acerto.
- **Disparo acima de cinco casas:** sofre −2 no acerto.
- **Passo sombrio:** crítico em 19–20 e +4 de defesa durante a resposta inimiga, mesmo se o golpe falhar.

A chance de acerto é calculada contando os resultados favoráveis entre as 20 faces. Um ataque de +4 contra defesa 14 tem 55% de acerto. A chance nunca fica abaixo de 5% nem acima de 95%, graças às regras de 1 e 20 naturais. Errar consome a energia e o turno da ação.

## Classes

| Classe | PV | Energia | FOR | DES | CON | INT | Dano básico | Custo inicial Q |
|---|---:|---:|---:|---:|---:|---:|---|---:|
| Guerreiro | 36 | 9 | 15 | 10 | 14 | 8 | 1d8 + FOR | 7 |
| Mago | 26 | 14 | 8 | 12 | 10 | 15 | 1d6 + INT | 10 |
| Arqueiro | 30 | 10 | 10 | 15 | 12 | 10 | 1d6 + DES | 8 |
| Ladino | 28 | 9 | 11 | 15 | 11 | 12 | 1d6 + DES | 8 |

Acerto e dano básicos dependem da arma: **espada usa FOR**, **adaga e arco usam DES**, **cajado usa INT**. As habilidades usam o atributo principal da classe: FOR para guerreiro, DES para ladino/arqueiro e INT para mago. Assim, um ladino com espada usa FOR no ataque básico e DES na habilidade. A proficiência entra no acerto; o modificador da arma entra tanto no acerto quanto no dano. Constituição fornece o modificador da defesa única, além do seu papel anterior na vida base/progressão. Destreza e Inteligência não aumentam a defesa diretamente.

Inimigos seguem a mesma regra: rato/goblin usam DES para acerto e dano; esqueleto/guardião usam FOR. A defesa é a CA natural + modificador de CON. Não existe uma defesa mágica separada.

O ataque básico de mago/arqueiro usa o cajado/arco equipado e não gasta energia, mesmo com energia zero. F escolhe a direção. Mago e arqueiro equipados com cajado/arco não atacam ao esbarrar. Quando desarmados, podem atacar corpo a corpo como as demais classes, causando 1d2. O alcance é de 6/10 casas e paredes bloqueiam. Desarmado, não é possível disparar; todas as classes causam 1d2 no corpo a corpo.

As habilidades causam `2d6 + atributo principal` (arqueiro: `2d8`), mais 1 de dano a cada dois níveis ganhos. Redemoinho alcança 2 passos; Nova arcana, 5; Flecha precisa, 10; Passo sombrio, 3. O custo cai em 1 a cada dois níveis ganhos, até 3 (mago: 4). A HUD mostra o custo e a fórmula atuais.

## Cura, recursos e dificuldade

Cada poção rola **dois dados independentes de dez faces**: cura de 2 a 20 PV, média 11. O diário mostra cada dado e a cura efetiva, limitada pela vida faltante. Beber consome um turno e permite a resposta inimiga. Com vida cheia, a poção não é consumida.

A expedição começa com uma arma comum equipada e 5 poções de vida, sem armadura ou acessório. Cada andar tem 75% de chance de conter uma poção e 40% de conter um cristal, nunca mais de um de cada. Há dois tesouros por andar; inimigos comuns têm 50% de chance de deixar ouro, com quantidade crescente por ciclo. Cristais recuperam 5 energia. Esperar recupera 1; a regeneração passiva é de 1 a cada seis turnos. Descer recupera apenas 2 PV e 3 energia.

Os monstros têm atributos, dados de dano, defesa e proficiência próprios. A população comum começa em 7, sobe a cada dois andares e para em 14. Andares de chefe têm dois inimigos comuns a menos, além do guardião. XP inicial: rato 2, esqueleto 3, goblin 4; aumenta em 1 a cada dois andares. O guardião concede `18 + 2 × andar` XP (28 no andar 5), uma poção, até 3 de energia, ouro e um equipamento garantido de raridade rara ou superior.

É necessário `30 + 20 × (nível − 1)` XP para subir. Cada nível concede somente **+1 atributo**: principal nos níveis pares, Constituição nos ímpares. O ganho de vida máxima é pequeno, de 2–3 na maior parte da expedição. Não há restauração completa de vida ou energia ao subir de nível. Experiência excedente é preservada. A seed é interna e não aparece na interface.

A proposta é uma expedição exigente: usar posição, linha de visão, habilidades e poções com cuidado é importante. Não é obrigatório eliminar todos os inimigos. Os andares são gerados proceduralmente sem um andar final. A cada múltiplo de cinco (5, 10, 15...), derrote o guardião para liberar a escada e continuar. O objetivo é alcançar o andar mais profundo possível. A morte encerra a partida e destaca o andar alcançado. O ouro pode ser gasto com o mercador. Os preços de compra seguem o valor do item; a venda rende metade, arredondada para baixo.

## Arte e código

As imagens originais e seus prompts estão em `ArtSources/`. A antiga tocha permanece apenas no arquivo de fontes, sem aparecer no menu. O Godot ignora os PNGs dessa pasta. O jogo carrega grades ASCII e oito faixas tonais de `Art/`, desenhadas com `DrawString`. Retratos reagem aos danos com flashes, tremor, caracteres de impacto e números flutuantes.

`python3 Tools/convert_ascii.py` reconstrói as grades a partir das imagens usando Pillow. Não é necessário para jogar. Ao exportar, inclua `Art/*.txt` e `Art/*.tone` no filtro de arquivos não-recursos.

- `Main.cs`: geração, turnos, recursos e estado.
- `TabletopRules.cs`: dados, probabilidades, atributos e críticos.
- `Balance.cs`: classes, progressão e resolução dos ataques.
- `CombatTests.cs`: testes das regras e auditorias estatísticas.
- `Presentation.cs`: HUD, menus, idiomas e desenho ASCII.
- `AsciiArt.cs`: catálogo das ilustrações convertidas.

Execute `./jogar.sh --headless -- --self-test`. Os testes cobrem 550 mapas, combate, cura 2d10, limites e média dos dados, críticos, chances de acerto, classes, escassez de itens, progressão, menus, idiomas e animações. A auditoria compara 20.000 ataques simulados à probabilidade de acerto calculada.

Fonte: DejaVu Sans Mono, licença em `FONT-LICENSE.txt`.

## English quick start

Run `./jogar.sh`, select **Idioma**, choose **English**, and confirm. Start a run and choose a class. Attacks roll **d20 + attribute modifier + proficiency** against defense. Natural 1 misses; 20 hits critically and doubles damage dice. I opens the inventory. Potions heal **2d10 (2–20 HP)** and consume a turn. Mage and Archer use F for free basic attacks with an equipped staff/bow. Each hero starts with a common weapon and 5 health potions. Q uses abilities; TAB selects a visible target; P drinks; E interacts with stairs or an adjacent merchant; Space waits; the Help tab is an adventure compendium; Esc pauses. Attributes grow slowly, supplies and XP are scarce.

## Progressão infinita

Poções continuam curando 2d10; itens comuns, cura de +2 PV e recuperação de +3 energia ao descer permanecem escassos. Os chefes fornecem uma reposição limitada, sem restaurar a vida inteira. Os testes exercitam 550 mapas, incluindo andares 5, 10, 15, 25, 50, 100, 500 e 1000, com chefes recorrentes, bloqueio de escadas e continuidade após derrotá-los.

## Comportamento dos inimigos

Inimigos comuns patrulham salas e corredores, avançando uma casa por turno. Ao avistar o jogador a até 12 casas, sem paredes bloqueando a visão, perseguem pelo caminho disponível e atacam quando adjacentes. Ao perder contato visual, buscam a última posição vista por até seis turnos; depois retomam a patrulha. Não enxergam a posição atual do jogador através das paredes, não atravessam outros inimigos e não se movem enquanto o jogo está pausado.

O guardião começa imóvel na escada. Ele só desperta quando o jogador entra na sala da escada, registrada durante a geração procedural. Seus movimentos ficam restritos a essa sala; não ataca o jogador do lado de fora. Quando o jogador sai, o chefe retorna à escada e volta a persegui-lo caso ele entre novamente. A escada permanece bloqueada até a derrota do chefe.

## Inventário, baús e equipamentos

Slots: **arma, armadura e acessório**. Itens ficam na mochila, inclusive os equipados (marcados com `[*]`). Cima/baixo seleciona; Enter equipa/remove ou bebe. A troca gasta um turno e retoma o jogo, permitindo a resposta dos inimigos. Não há limite de mochila; a lista é paginada. A ficha reflete os bônus ativos. A ordem das abas é Personagem (1), Inventário (2), Diário (3), Ajuda (4), Configurações (5).

- Espada: guerreiro/ladino, **1d8** comum. Adaga: guerreiro/ladino, **1d6**.
- Cajado: mago, **1d6**, usa INT. Arco: arqueiro, **1d6**, usa DES.
- Armadura de placas: guerreiro, CA base 14 e bônus FOR/CON; couro: qualquer classe, CA 12 e bônus DES; manto: mago, CA 11 e bônus INT. A CA ganha +1 a cada dois pontos de poder do item.
- Amuleto: qualquer classe; aumenta diretamente FOR, DES, CON e INT em 1 + poder do item. Os modificadores são recalculados, sem acumular bônus ao reequipar.
- Poção de vida: **2d10 PV**, valor 12 ouro. Poção de energia: **2d6 EN**, valor 15 ouro. Não são consumidas com o recurso cheio.

**Comum (cinza), raro (azul), épico (roxo), lendário (dourado)** requerem níveis 1/3/6/10. A arma ganha +2 faces no dado e +1 de dano fixo por raridade. Equipamentos obtidos a cada 20 andares também ganham um grau de poder (+1 bônus e requisito de nível). Requisitos de classe e nível são verificados ao equipar.

Épicos: arma adiciona **1d4** ao acertar; armadura reduz dano em **1**; amuleto recupera **1 EN por abate**. Lendários: arma adiciona **1d6** e drena até **2 PV**; armadura reduz dano em **2**; amuleto recupera **2 EN por abate**. Redução preserva pelo menos 1 de dano recebido. Efeitos e valores estão descritos no inventário.

Em andares de combate, a chance de um baú começa em **42%**, ganha 1 ponto percentual por ciclo e para em 60%. Cada baú dá uma recompensa e tem +3 pontos percentuais por ciclo de dar uma segunda, até 35%. Cada recompensa é equipamento (70%) ou poção (30%). No primeiro ciclo, os equipamentos têm 15% de chance de serem raros; a cada ciclo, raros ganham 2 pontos percentuais (limite 40%), épicos 2 (25%) e lendários 1 (15%); o restante é comum. Não há baús no refúgio do mercador.

O primeiro ciclo corresponde aos andares 1–5; o índice `c` começa em zero. Inimigos comuns que deixam ouro dão de `1+c` a `2+2c` moedas. O guardião garante de `18+8c` a `28+8c` moedas e um equipamento: lendário com `min(35, 2+3c)%`, épico com `min(45, 10+3c)%` e raro no restante. O primeiro guardião, por exemplo, garante 18–28 moedas e pelo menos um raro. Os itens mantêm requisitos de classe e nível.

Ataques inimigos exigem uma casa de distância ortogonal (sem diagonais). Ao se aproximar de um inimigo e entrar em adjacência, o jogador não recebe ataque nessa mesma ação; o inimigo pode atacar na próxima ação se continuar adjacente. Inimigos que se movem até o jogador também não atacam no mesmo turno do movimento.

A aba Ajuda, após Diário, organiza o compêndio da aventura em sete categorias navegáveis com cima/baixo. O atalho H foi removido. As artes da entrada, fogueira e livro do diário têm iluminação suave animada nos próprios caracteres ASCII, inclusive com o jogo pausado. A ficha só apresenta os controles de disparo às classes que podem usar arco/cajado.

## Mercador e confirmações

Depois do primeiro andar, cada andar que não é de chefe tem **10% de chance** de ser um refúgio do mercador. Chefes nos múltiplos de cinco são preservados. O refúgio é uma única sala com o mercador `M` e a escada `>`, sem monstros, baús ou itens no chão.

Fique ao lado de `M` e pressione **E** para negociar. A/D, setas laterais ou Tab alternam Comprar/Vender; cima/baixo seleciona. Cada compra e venda requer confirmação explícita, com **Cancelar selecionado inicialmente**. Poções são negociadas uma por vez. Falta de ouro impede a compra. Comprar um equipamento não o equipa; requisitos continuam sendo exigidos no inventário. Vender uma peça equipada a remove do personagem, com aviso na confirmação.

O estoque contém seis equipamentos aleatórios e quantidades limitadas de poções de vida e energia. Usa as raridades disponíveis na profundidade atual e não é sorteado novamente ao reabrir a loja. Itens vendidos ficam disponíveis para recompra pelo valor integral. Comprar e vender no refúgio não avança turnos.

Voltar ao menu pela pausa também exige confirmação. O aviso informa que o progresso não será salvo, e cancelar preserva a expedição. O compêndio agora contém apenas informações da aventura, sem fórmulas de combate ou características técnicas do jogo.

O retrato do mercador foi gerado com a ferramenta integrada imagegen e convertido para ASCII. Fonte: `ArtSources/merchant.png`; prompt final: `ArtSources/merchant-prompt.txt`; arte em uso: `Art/merchant.txt` e `Art/merchant.tone`.
