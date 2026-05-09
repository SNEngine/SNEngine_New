import * as monaco from 'monaco-editor'

export function registerSnLanguage() {
  monaco.languages.register({ id: 'sn' })

  monaco.languages.setMonarchTokensProvider('sn', {
    keywords: [
      'if', 'else', 'endif', 'switch', 'switchcase', 'default', 'endswitch',
      'function', 'endfunc', 'call', 'print', 'Quit', 'for', 'endfor',
      'Jump To', 'name:', 'playerHealth'
    ],
    tokenizer: {
      root: [
        [/\b(if|else|endif|switch|function|call|print|Quit|Jump To)\b/, 'keyword'],
        [/[a-zA-Z_]\w*/, 'variable'],
        [/"(.*?)"/, 'string'],
        [/\d+/, 'number'],
        [/\/\/.*/, 'comment'],
      ]
    }
  })

  // === КАСТОМНАЯ ТЕМА С ЦВЕТАМИ ===
  monaco.editor.defineTheme('sn-dark', {
    base: 'vs-dark',
    inherit: true,
    rules: [
      { token: 'keyword', foreground: 'C586C0', fontStyle: 'bold' },   // фиолетовый
      { token: 'variable', foreground: '9CDCFE' },                     // голубой
      { token: 'string', foreground: 'CE9178' },                       // оранжевый
      { token: 'number', foreground: 'B5CEA8' },                       // салатовый
      { token: 'comment', foreground: '6A9955', fontStyle: 'italic' }  // зелёный
    ],
    colors: {
      'editor.background': '#1E1E1E',
      'editor.foreground': '#D4D4D4',
      'editorLineNumber.foreground': '#858585',
      'editorCursor.foreground': '#AEAFAD'
    }
  })
}