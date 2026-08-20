# DM UI Framework

Каркас MVVM для UI на Unity UI Toolkit: `Widget` / `IViewModel` / `UILayout`,
пулинг, реактивные типы, интеграция с VContainer и Addressables.

## Состав пакета

Один пакет, шесть сборок:

| Сборка | Назначение |
|---|---|
| `DM.UI` | Ядро MVVM: `IViewModel`, `UILayout`, `Widget`, система анимаций UI, `UISystem`, `UIRoot`. |
| `DM.UI.UIToolkit` | Реализация `UILayout` под `PanelRenderer`/`VisualElement` (`UITKLayout`). |
| `DM.UI.DOTween` | Адаптер анимаций DOTween для UI Toolkit. Закрыта `defineConstraints: ["DM_DOTWEEN"]` — без платного DOTween Pro и определённого символа `DM_DOTWEEN` сборка исключается из компиляции, а не падает ошибкой. |
| `DM.Pooling` | Универсальный пул объектов и компонентов (`IPool`, `Pool`, `GameObjectPool`, `ComponentPool`). |
| `DM.Integration` | Мост к VContainer и Addressables: провайдер лэйаутов, реестр лэйаутов, провайдер моделей. |
| `DM.Reactivity` | Реактивные типы (`ReactiveProperty`, `ReactiveList`, `ReactiveDictionary`) и подписки. |

## Предусловия

Пакет объявляет как зависимость только `com.unity.addressables` — UPM умеет
ссылаться только на пакеты реестра. Две зависимости приходят по git-ссылкам
и **не могут** быть объявлены в `package.json`, поэтому это предусловие,
а не деталь: проект, подключающий `com.dm.ui`, обязан сам подключить

- **UniTask** — `https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask`
- **VContainer** — `https://github.com/hadashiA/VContainer.git?path=VContainer/Assets/VContainer`

Без них `DM.UI` и `DM.Integration` не скомпилируются.

Для сборки `DM.UI.DOTween` дополнительно нужен установленный **DOTween Pro**
и определённый в проекте символ `DM_DOTWEEN`. Пакет не определяет его сам:
без платного DOTween это дало бы ошибку компиляции у всех, кто его не купил.
Пока символ не определён, сборка просто не участвует в компиляции.

## Контракт MVVM

Экран — ровно три файла: `UILayout` хранит только ссылки на элементы
и обнуляет их при возврате в пул, `IViewModel` выставляет состояние,
`Widget` подписывается и пишет в разметку.

- **Поток состояния:** игра → модель → вид. `Tick()` у `ITickableViewModel` —
  единственное место, где читается сервис; оттуда состояние перекладывается
  в `ReactiveProperty` из `DM.Reactivity`. Опрашивать сервис из виджета
  запрещено — иначе тик расползается по двум местам.
- **`IViewModel` не знает про вид:** ни `VisualElement`, ни `UILayout`,
  ни `Widget` — ни в полях, ни в параметрах, ни в сигнатурах. Команды
  у модели разрешены (`StartMission()`), управление видом — нет.
- **Подписки живут на стороне `Widget`:** `AddDisposable` в `OnInitialize`,
  снятие в `DeInitialize`. Модель не хранит ссылку на подписчика.
- **Ловушка `Reset()`:** у пулируемой модели (`IResettableViewModel`)
  `Dispose()` не вызывается вовсе, вызывается `Reset()`. `ClearSubscribers()`
  внутри `Reset()` **маскирует** баг «виджет не отписался», а не чинит его.

Эти правила — не пожелания, а условия, при которых каркас работает как задумано:
пул переиспользует лэйауты, поэтому висящая ссылка на мёртвый элемент или
неснятая подписка проявляются не сразу и не там, где допущены.

## Установка

Через UPM по git-ссылке, в `Packages/manifest.json` проекта:

```json
"com.dm.ui": "https://github.com/DarkMasster/dm-ui.git#v0.1.0"
```

Тег в ссылке обязателен. Без него UPM берёт ветку по умолчанию, и «версия»
пакета начинает зависеть от даты сборки: два разработчика с одинаковым
манифестом получат разный код.

Для локальной разработки самого каркаса — по `file:`-ссылке, путь относительно
папки `Packages` целевого проекта:

```json
"com.dm.ui": "file:../../dm-ui"
```
