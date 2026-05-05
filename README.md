# Arch Mage

<img width="1221" height="593" alt="Image" src="https://github.com/user-attachments/assets/c1d5aaf8-23d9-4774-91f9-601884e9cc03" />

<br> </br>

> *"As escolhas custam."*

Jogo 2D top-down em pixel art feito em Unity. Tudo começa em uma mesa de RPG no mundo real — e termina nas profundezas de um planeta azul misterioso.

---

## O que é o Jogo?

Arch Mage é um **action game de arenas** focado em ciclos curtos de combate, leitura de padrões de inimigos e decisões de build simples. É um projeto de estudo e protótipo jogável (MVP), mas com narrativa suficiente para conectar o jogador à figura do Mestre e às consequências de suas escolhas.

A premissa: um Mestre de RPG apresenta um planeta que *"não devia estar ali"*. Algo despertou em seu interior. Cabe ao jogador descer até o Núcleo e descobrir o que é.

---

## Personagens Jogáveis

O jogador escolhe entre duas fichas colocadas pelo Mestre sobre a mesa:

| Classe | Estilo | Pontos Fortes | Fraquezas |
|---|---|---|---|
| **Mago** | Distância / Área | Dano em grupo, controle de área | Frágil no corpo a corpo |
| **Paladino** | Linha de frente | Vida alta, dano crítico em alvo único, dash/evasão | Exige timing, sem dano em área |

---

## Estrutura do MVP

Cinco fases lineares dentro do planeta azul, cada uma com papel claro na curva de aprendizado:

```
Mesa do Mestre → Escolha de Personagem → Queda no Planeta

  Fase 1 – Tutorial          (movimento, ataque, slimes básicos)
      ↓
  Fase 2 – Superfície        (inimigos à distância, elite introduzido)
      ↓
  Fase 3 – Catedral de Gelo  (sobrevivência em ondas → mini-boss)
      ↓
  Fase 4 – O Livro de Regras (desafio de selos + elites)
      ↓
  Fase 5 – O Narrador        (boss final, duas etapas)
```

### Fase 1 — A Queda no Planeta Azul (Tutorial)
Arena curta com 1 sala principal e 2 ondas de slimes. Foco total em ensinar movimento e ataque. Punição leve.

### Fase 2 — Sinais na Superfície
Pequenas arenas com inimigos de ataque à distância. Introdução de um **elite** com projéteis mais rápidos e ataques telegrafados.

### Fase 3 — Catedral de Gelo/Metal
Arena única de sobrevivência em ondas: inimigos de gelo, sentinelas à distância e o **Coração da Catedral** como mini-boss.

### Fase 4 — O Livro de Regras
Desafio especial: ativar 3 selos/placas de pressão enquanto ondas de inimigos não param. Elites na reta final, baús com recompensas melhores.

### Fase 5 — O Narrador (Boss Final)
Batalha em duas etapas numa grande arena. Padrões de projéteis, invocação de minions e fase agressiva com golpes telegrafados.

---

## Mecânica de Narração

A voz do Mestre funciona como narrador pontual ao longo do jogo:

- Falas curtas no **início**, em **gatilhos importantes** e no **fim** de cada fase.
- **Comentários reativos** quando o jogador morre, usa certas habilidades ou toma decisões específicas.
- Variações de fala por **classe escolhida**, reforçando a sensação de mesa de RPG narrada.

---

## Tecnologias

| Camada | Tecnologia |
|---|---|
| Engine | Unity 2D (top-down) |
| Linguagem | C# |
| Arte | Pixel art — personagens, inimigos, cenários |

---

## Como rodar

**1. Clonar o repositório**

```bash
git clone https://github.com/seu-usuario/arch-mage.git
cd arch-mage
```

**2. Abrir no Unity**

Abra o projeto pela **Unity Hub** usando uma versão LTS recente com suporte a projetos 2D.

**3. Iniciar**

Abra a cena inicial (`MainMenu` ou `PlanetIntro`) e pressione **Play** no Editor.

---

## Controles (Provisório)

| Ação | Input |
|---|---|
| Mover | `WASD` ou setas |
| Mirar / Atacar | Mouse |

> Controles adicionais serão documentados conforme o desenvolvimento avança.
