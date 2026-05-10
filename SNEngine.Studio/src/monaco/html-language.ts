import * as monaco from 'monaco-editor'

export function registerHtmlLanguage() {
  monaco.editor.defineTheme('html-dark', {
    base: 'vs-dark',
    inherit: true,
    rules: [
      { token: 'tag', foreground: '#569CD6', fontStyle: 'bold' },           // Теги <div>, <h1>
      { token: 'tag.name', foreground: '#569CD6', fontStyle: 'bold' },
      { token: 'attribute.name', foreground: '#9CDCFE' },                  // Атрибуты (class, id, onclick)
      { token: 'attribute.value', foreground: '#CE9178' },                 // Значения атрибутов
      { token: 'string', foreground: '#CE9178' },                          // Строки
      { token: 'comment', foreground: '#6A9955', fontStyle: 'italic' },    // Комментарии
      { token: 'delimiter', foreground: '#D4D4D4' },
      { token: 'metatag', foreground: '#D7BA7D' }
    ],
    colors: {
      'editor.background': '#1E1E1E',
      'editor.foreground': '#D4D4D4',
      'editorLineNumber.foreground': '#858585'
    }
  })
}