// src/composables/useDirectoryTreeContextMenu.ts
import type { Ref } from 'vue'

export interface ContextMenuItem {
  label?: string
  icon?: string
  action?: () => void | Promise<void>
  type?: 'separator'
  danger?: boolean
}

export interface BuildContextMenuOptions {
  item: any
  currentSelected: any[]
  count: number
  createItem: (base: string, isFolder: boolean, selected?: any) => void
  renameItem: (item: any) => void
  duplicateItem: (path: string) => void
  deleteItem: (item: any) => void
  deleteItems: (items: any[]) => void
  showProperties: (item: any) => void
  openWith: (path: string) => void
  showInExplorer: (path: string) => void
  copyPath: (path: string) => void
  copyName: (path: string) => void
  basePath: string
}

/**
 * Builds context menu items for DirectoryTree.
 * Extracted to keep DirectoryTree.vue smaller.
 */
export function useDirectoryTreeContextMenu() {
  function buildContextMenuItems(opts: BuildContextMenuOptions): ContextMenuItem[] {
    const {
      item,
      currentSelected,
      count,
      createItem,
      renameItem,
      duplicateItem,
      deleteItem,
      deleteItems,
      showProperties,
      openWith,
      showInExplorer,
      copyPath,
      copyName,
      basePath,
    } = opts

    if (!item) {
      // Background menu
      const selected = currentSelected
      const targetFolder = selected.find((i: any) => i.isFolder) || selected[0]
      const target = targetFolder?.isFolder ? targetFolder.path : basePath

      return [
        { label: 'Создать файл', icon: 'file_icon', action: () => createItem(target, false) },
        { label: 'Создать папку', icon: 'folder_icon', action: () => createItem(target, true) },
        { type: 'separator' },
        { label: 'Открыть в проводнике', icon: 'explorer_icon', action: () => showInExplorer(target) },
      ]
    }

    // Item-specific menu
    const menu: ContextMenuItem[] = [
      { label: 'Создать файл', icon: 'file_icon', action: () => createItem(basePath, false, item) },
      { label: 'Создать папку', icon: 'folder_icon', action: () => createItem(basePath, true, item) },
    ]

    if (count > 1) {
      menu.push(
        { type: 'separator' },
        { label: `Удалить ${count} элементов`, icon: 'error_icon', action: () => deleteItems(currentSelected), danger: true },
        { label: `Дублировать ${count} элементов`, icon: 'copy_icon', action: () => currentSelected.forEach((it: any) => duplicateItem(it.path)) },
        { type: 'separator' },
        {
          label: 'Копировать пути',
          icon: 'info_icon',
          action: () => navigator.clipboard.writeText(currentSelected.map((i: any) => i.path).join('\n')),
        }
      )
    } else {
      menu.push(
        { type: 'separator' },
        { label: 'Переименовать', icon: 'edit_icon', action: () => renameItem(item) },
        { label: 'Дублировать', icon: 'copy_icon', action: () => duplicateItem(item.path) },
        { label: 'Свойства', icon: 'info_icon', action: () => showProperties(item) },
        { label: 'Открыть с помощью...', icon: 'open_with_icon', action: () => openWith(item.path) },
        { label: 'Удалить', icon: 'error_icon', action: () => deleteItem(item), danger: true },
        { type: 'separator' },
        { label: 'Показать в проводнике', icon: 'explorer_icon', action: () => showInExplorer(item.path) },
        { label: 'Копировать путь', icon: 'info_icon', action: () => copyPath(item.path) },
        { label: 'Копировать имя', icon: 'info_icon', action: () => copyName(item.path) }
      )
    }

    return menu
  }

  return {
    buildContextMenuItems,
  }
}
