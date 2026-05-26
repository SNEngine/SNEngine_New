import { ref, computed, reactive } from 'vue'

export interface Tab {
  id: string
  filePath: string
  name: string
  type: string
  content?: string
  language?: string
  isDirty: boolean
  isDeleted?: boolean
  icon?: string
  previewOptions?: any
}

export interface EditorGroup {
  id: string
  tabs: Tab[]
  activeFilePath: string | null
}

// --- Singleton state for useTabs (must be shared across EditorTabs + all TabGroup instances) ---
let _tabsInstance: ReturnType<typeof _createTabs> | null = null

function _createTabs() {
  const groups = ref<EditorGroup[]>([
    reactive({ 
      id: 'group-main', 
      tabs: reactive<Tab[]>([]), 
      activeFilePath: null 
    } as EditorGroup)
  ])

  const activeGroupId = ref<string>('group-main')

  const findGroup = (id: string) => groups.value.find(g => g.id === id)

  const setActiveGroup = (groupId: string) => {
    if (findGroup(groupId)) activeGroupId.value = groupId
  }

  const createGroup = (afterGroupId?: string): string => {
    const newId = 'group-' + Date.now().toString(36)
    const newG: EditorGroup = reactive({ 
      id: newId, 
      tabs: reactive<Tab[]>([]), 
      activeFilePath: null 
    } as EditorGroup)
    if (afterGroupId) {
      const idx = groups.value.findIndex(g => g.id === afterGroupId)
      if (idx > -1) {
        groups.value.splice(idx + 1, 0, newG)
      } else {
        groups.value.push(newG)
      }
    } else {
      groups.value.push(newG)
    }
    return newId
  }

  const closeGroup = (groupId: string) => {
    const idx = groups.value.findIndex(g => g.id === groupId)
    if (idx === -1) return
    groups.value.splice(idx, 1)
    if (activeGroupId.value === groupId) {
      if (groups.value.length === 0) {
        groups.value.push(reactive({ 
          id: 'group-main', 
          tabs: reactive<Tab[]>([]), 
          activeFilePath: null 
        } as EditorGroup))
        activeGroupId.value = 'group-main'
      } else {
        activeGroupId.value = groups.value[0].id
      }
    }
  }

  const removeEmptyGroupIfPossible = (groupId: string) => {
    if (groups.value.length <= 1) return
    const g = findGroup(groupId)
    if (g && g.tabs.length === 0) {
      const idx = groups.value.findIndex(gr => gr.id === groupId)
      if (idx > -1) {
        groups.value.splice(idx, 1)
        if (activeGroupId.value === groupId) {
          activeGroupId.value = groups.value[0]?.id || 'group-main'
        }
      }
    }
  }

  const openTabInGroup = (groupId: string, tabData: Partial<Tab> & { filePath: string }) => {
    let group = findGroup(groupId)
    if (!group) group = groups.value[0]
    if (!group) return

    const filePath = tabData.filePath.replace(/\\+/g, '/').replace(/\/+$/, '')

    // Check if already open in this group
    let existing = group.tabs.find(t => t.filePath.replace(/\\+/g, '/').replace(/\/+$/, '') === filePath)
    if (existing) {
      if (existing.isDeleted) existing.isDeleted = false
      group.activeFilePath = existing.filePath
      setActiveGroup(group.id)
      return
    }

    const newTab: Tab = {
      id: Date.now().toString(36) + Math.random().toString(36).slice(2),
      filePath,
      name: tabData.name || filePath.split(/[/\\]/).pop() || filePath,
      type: tabData.type || 'unknown',
      content: tabData.content || '',
      language: tabData.language,
      isDirty: !!tabData.isDirty,
      isDeleted: false,
      icon: tabData.icon,
      previewOptions: tabData.previewOptions
    }

    group.tabs.push(newTab)
    group.activeFilePath = newTab.filePath
    setActiveGroup(group.id)
  }

  const closeTab = (tab: Tab | string, groupId?: string) => {
    let g = groupId ? findGroup(groupId) : findGroup(activeGroupId.value)
    if (!g) {
      // fallback: search all groups
      const searchPath = typeof tab === 'string' ? tab : tab.filePath
      for (const gg of groups.value) {
        if (gg.tabs.some(t => t.filePath === searchPath || t.id === (typeof tab === 'string' ? '' : tab.id))) {
          g = gg
          break
        }
      }
    }
    if (!g) return

    const filePath = typeof tab === 'string' ? tab : tab.filePath
    const tabId = typeof tab === 'string' ? null : tab.id
    const idx = g.tabs.findIndex(t => t.id === tabId || t.filePath === filePath)
    if (idx === -1) return

    const wasActive = g.activeFilePath === g.tabs[idx].filePath
    g.tabs.splice(idx, 1)

    if (wasActive) {
      g.activeFilePath = g.tabs.length > 0 ? g.tabs[Math.max(0, idx - 1)].filePath : null
    }

    removeEmptyGroupIfPossible(g.id)
  }

  const splitTabToNewGroup = (sourceGroupId: string, tab: Tab) => {
    const src = findGroup(sourceGroupId)
    if (!src) return
    const tIdx = src.tabs.findIndex(t => t.id === tab.id)
    if (tIdx === -1) return

    const wasActive = src.activeFilePath === tab.filePath
    src.tabs.splice(tIdx, 1)
    if (wasActive) {
      src.activeFilePath = src.tabs.length ? src.tabs[Math.max(0, tIdx - 1)]?.filePath || null : null
    }

    const newGId = createGroup(sourceGroupId)
    const newG = findGroup(newGId)!
    newG.tabs.push({ ...tab })
    newG.activeFilePath = tab.filePath
    activeGroupId.value = newGId

    removeEmptyGroupIfPossible(sourceGroupId)
  }

  /**
   * Move a tab from one group to another (or reorder inside the same group).
   * Used for drag & drop between panes.
   */
  const moveTabToGroup = (
    tab: Tab | string | { id?: string; filePath?: string },
    fromGroupId: string,
    toGroupId: string,
    targetIndex?: number
  ) => {
    const fromG = findGroup(fromGroupId)
    const toG = findGroup(toGroupId)
    if (!fromG || !toG) return

    // Support: real Tab, string (treated as ID), or {id, filePath} from drag payload
    let searchId: string | null = null
    let searchPath: string | null = null

    if (typeof tab === 'string') {
      searchId = tab
    } else if (tab && typeof tab === 'object') {
      searchId = (tab as any).id ?? null
      searchPath = (tab as any).filePath ?? null
    }

    const fromIdx = fromG.tabs.findIndex(t =>
      (searchId && t.id === searchId) || (searchPath && t.filePath === searchPath)
    )
    if (fromIdx === -1) {
      console.warn('[useTabs] moveTabToGroup: tab not found in source group', { searchId, searchPath, fromGroupId })
      return
    }

    const movingTab = fromG.tabs[fromIdx]
    const wasActiveInFrom = fromG.activeFilePath === movingTab.filePath

    // Remove from source
    fromG.tabs.splice(fromIdx, 1)
    if (wasActiveInFrom) {
      fromG.activeFilePath = fromG.tabs.length > 0
        ? fromG.tabs[Math.max(0, fromIdx - 1)].filePath
        : null
    }

    // Insert into target (default to end)
    let insertAt = targetIndex ?? toG.tabs.length
    insertAt = Math.max(0, Math.min(insertAt, toG.tabs.length))

    toG.tabs.splice(insertAt, 0, { ...movingTab })

    // Activate in target group
    toG.activeFilePath = movingTab.filePath
    activeGroupId.value = toG.id

    // Force reactivity so every TabGroup's computed `group` and TabBar re-render
    groups.value = [...groups.value]

    // Clean up empty source group (if it became empty)
    removeEmptyGroupIfPossible(fromGroupId)
  }

  const markDirty = (filePath: string) => {
    const norm = filePath.replace(/\\+/g, '/').replace(/\/+$/, '')
    for (const g of groups.value) {
      const t = g.tabs.find(tt => tt.filePath.replace(/\\+/g, '/').replace(/\/+$/, '') === norm)
      if (t) t.isDirty = true
    }
  }

  const markClean = (filePath: string) => {
    const norm = filePath.replace(/\\+/g, '/').replace(/\/+$/, '')
    for (const g of groups.value) {
      const t = g.tabs.find(tt => tt.filePath.replace(/\\+/g, '/').replace(/\/+$/, '') === norm)
      if (t) t.isDirty = false
    }
  }

  const markAsDeleted = (filePath: string) => {
    const norm = filePath.replace(/\\+/g, '/').replace(/\/+$/, '')
    for (const g of groups.value) {
      const t = g.tabs.find(tt => tt.filePath.replace(/\\+/g, '/').replace(/\/+$/, '') === norm)
      if (t) t.isDeleted = true
    }
  }

  const findGroupWithPreview = () => {
    const previewPath = '::preview::'
    for (const g of groups.value) {
      if (g.tabs.some(t => t.filePath === previewPath)) return g.id
    }
    return null
  }

  // Legacy flat views (point to active group) for minimal compat during transition
  const tabs = computed(() => {
    const ag = findGroup(activeGroupId.value)
    return ag ? ag.tabs : []
  })

  const activeFilePath = computed(() => {
    const ag = findGroup(activeGroupId.value)
    return ag ? ag.activeFilePath : null
  })

  const activateTab = (tab: Tab) => {
    for (const g of groups.value) {
      const found = g.tabs.find(t => t.id === tab.id || t.filePath === tab.filePath)
      if (found) {
        g.activeFilePath = found.filePath
        activeGroupId.value = g.id
        return
      }
    }
  }

  const hasUnsavedChanges = () => {
    return groups.value.some(g => g.tabs.some(t => t.isDirty))
  }

  const getAllDirtyTabs = () => {
    const result: Array<{ groupId: string; tab: Tab }> = []
    for (const g of groups.value) {
      for (const t of g.tabs) {
        if (t.isDirty && !t.isDeleted) {
          result.push({ groupId: g.id, tab: { ...t } })
        }
      }
    }
    return result
  }

  return {
    // Modern pane API
    groups,
    activeGroupId,
    setActiveGroup,
    createGroup,
    closeGroup,
    openTabInGroup,
    closeTab,
    splitTabToNewGroup,
    moveTabToGroup,
    removeEmptyGroupIfPossible,
    findGroupWithPreview,
    hasUnsavedChanges,
    getAllDirtyTabs,

    // Legacy / compat (used by current EditorTabs during migration)
    tabs,
    activeFilePath,
    activateTab,
    markDirty,
    markClean,
    markAsDeleted
  }
}

export function useTabs() {
  if (!_tabsInstance) {
    _tabsInstance = _createTabs()
  }
  return _tabsInstance
}
