import * as monaco from 'monaco-editor'

export function registerSnEngineTheme() {
  monaco.editor.defineTheme('snengine-dark', {
    base: 'vs-dark',
    inherit: true,
    rules: [
      // Основные акценты — красный
      { token: 'keyword', foreground: '#FF3B5C', fontStyle: 'bold' },           // if, function, print и т.д.
      { token: 'special-command', foreground: '#FF1744', fontStyle: 'bold' },   // Show Character, Quit, Jump To
      { token: 'call-keyword', foreground: '#FF5252', fontStyle: 'bold' },      // call
      { token: 'native-block', foreground: '#FF1744', fontStyle: 'bold' },      // native / endnative

      // Другие элементы
      { token: 'function', foreground: '#FF6B6B' },
      { token: 'string', foreground: '#FFCC80' },                               // тёплый оранжевый
      { token: 'number', foreground: '#FF8A80' },
      { token: 'comment', foreground: '#6D7278', fontStyle: 'italic' },         // приглушённый серый
      { token: 'variable', foreground: '#B0BEC5' },
      { token: 'tag', foreground: '#FF5252', fontStyle: 'bold' },
      { token: 'attribute.name', foreground: '#FF8A80' },
      { token: 'attribute.value', foreground: '#FFCC80' },
    ],
    colors: {
      'editor.background': '#1F1F1F',           // тёмно-серый фон
      'editor.foreground': '#E0E0E0',
      'editorLineNumber.foreground': '#757575',
      'editorCursor.foreground': '#FF1744',     // ярко-красный курсор
      'editor.selectionBackground': '#FF174440',
      'editor.selectionForeground': '#FFFFFF',
      'editor.findMatchBackground': '#FF1744',
      'editor.findMatchHighlightBackground': '#FF174480',
      'editor.hoverHighlightBackground': '#FF174420'
    }
  })
}