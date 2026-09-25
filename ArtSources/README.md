# Origens das artes

Todas as 17 imagens nesta pasta foram criadas com a ferramenta integrada **imagegen**, sem usar API/CLI externo. Os prompts originais estão em [prompts.json](prompts.json); o novo retrato usa [merchant-prompt.txt](merchant-prompt.txt).

- Heróis: `warrior.png`, `mage.png`, `archer.png`, `rogue.png`.
- Monstros: `rat.png`, `skeleton.png`, `goblin.png`, `warden.png`.
- Menus: `tower.png`, `camp.png`, `globe.png`, `book.png`, `grave.png`, `crown.png`.
- Complementos: `unknown.png`, `torch.png`.

As bases são ilustrações detalhadas monocromáticas de fantasia. `../Tools/convert_ascii.py` converte luminância e contornos em caracteres ASCII, corrige a proporção dos glifos e escreve as grades e oito faixas tonais em `../Art/`. As fontes permanecem aqui para permitir novas conversões. `.gdignore` impede que o jogo importe os PNGs como recursos visuais: o resultado exibido é sempre texto desenhado pelo Godot.

- Mercador: `merchant.png`, retrato gerado pela ferramenta integrada imagegen e convertido para 100 x 60 caracteres.
