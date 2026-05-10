import * as monaco from 'monaco-editor'

export function registerCssLanguage() {
  monaco.editor.defineTheme('css-dark', {
    base: 'vs-dark',
    inherit: true,
    rules: [
      { token: 'selector', foreground: '#D7BA7D' },                        // Селекторы
      { token: 'property', foreground: '#9CDCFE' },                        // Свойства
      { token: 'value', foreground: '#CE9178' },                           // Значения
      { token: 'number', foreground: '#B5CEA8' },
      { token: 'string', foreground: '#CE9178' },
      { token: 'comment', foreground: '#6A9955', fontStyle: 'italic' },
      { token: 'delimiter', foreground: '#D4D4D4' }
    ],
    colors: {
      'editor.background': '#1E1E1E',
      'editor.foreground': '#D4D4D4'
    }
  })
}