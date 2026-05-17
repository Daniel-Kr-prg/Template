# DanieloZ.WorldInteraction

Модуль физических world-space интерактивов: кнопки, тумблеры, переносимые предметы для слотов и слайдеры.

## Базовые компоненты

- `WorldInteractionController` - единая точка ввода для drag/use/hover/swing через raycast.
- `WorldDraggable` - переносимый объект с pickup-анимацией, инерцией, вращением колесом мыши и optional constraints.
- `World3DSlotItem` - переносимый объект с одним `ItemId`, который можно вставлять в слот.
- `World3DPhysicalButton` - совместимый алиас старого имени; по смыслу это теперь slot item, а не кнопка.
- `World3DButtonSlotBase` - слот с набором принимаемых `ItemId`. При удержании item слот реагирует на hover курсором: accepted/rejected indicators показывают, можно ли вставить текущий item. Вставка происходит на `MouseUp`, если курсор находится над подходящим слотом. Программный `TryInsert` оставлен для групп и скриптов.
- `World3DStaticButton` - обычная нажимаемая 3D-кнопка.
- `World3DToggleButton` - самостоятельный 3D-toggle без группы.
- `World3DToggleGroup` + `World3DToggleObject` - группа состояний с физическим marker item и слотами.
- `World3DSlider` - слайдер, который вычисляет значение по позиции handle вдоль локальной оси; handle обычно является `WorldDraggable`/`World3DSlotItem` с constraints.

## Слот

1. На объект слота добавьте trigger collider и `World3DButtonSlotBase`.
2. Укажите `Anchor`, куда будет snapped item.
3. Заполните `acceptedItemIds`; пустой список принимает любые item.
4. На переносимый объект добавьте `World3DSlotItem` или совместимый `World3DPhysicalButton`, задайте один `ItemId`.

Чтобы вставить предмет, игрок должен взять item, навести курсор на слот и отпустить ЛКМ. Само столкновение item с collider слота ничего не вставляет; collider нужен только как raycast/hover область.

## Тестовые prefabs

В `Examples/Prefabs` лежат минимальные prefabs:

- `Base3DStaticButton.prefab`
- `Base3DStaticToggleButton.prefab`
- `Base3DStaticToggleGroup.prefab`
- `Base3DPhysicalButtonSlot.prefab`
- `Base3DPhysicalButton.prefab`
- `Test3DSlotItem.prefab`
- `Test3DSlot.prefab`
- `Test3DStaticButton.prefab`
- `Test3DToggleButton.prefab`
- `Test3DSlider.prefab`

`Base3D...` prefabs перенесены из проектной папки WorldUI, а `Test3D...` prefabs служат компактными standalone-примерами новых компонентов. Они используют компоненты этого модуля и стандартные primitive meshes. `Examples/Scenes/WorldInteractionExamples.unity` содержит все эти элементы на одной сцене.

`Examples/Scripts/PixelVoxelPuzzleIntegration` содержит bridge-компоненты для текущей hand/use системы проекта PixelVoxelPuzzle. Они сохранены рядом с examples, потому что это интеграционный слой, а не ядро модуля.
