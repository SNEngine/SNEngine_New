import * as monaco from 'monaco-editor'

export function registerSnLanguage() {
  monaco.languages.register({ id: 'sn' })

  monaco.languages.setMonarchTokensProvider('sn', {
    keywords: [
      'if', 'else', 'endif', 'switch', 'switchcase', 'default', 'endswitch',
      'for', 'endfor', 'while', 'endwhile', 'function', 'endfunc', 'print', 'Quit'
    ],

    specialCommands: [
      'Show Character', 'Hide Character', 'Show Background', 'Hide Background',
      'Play Music', 'Stop Music', 'Play Sound', 'Jump To', 'Call Scene'
    ],

    tokenizer: {
      root: [
        // Специальные многословные команды
        [/\b(Show Character|Hide Character|Show Background|Hide Background|Play Music|Stop Music|Play Sound|Jump To|Call Scene)\b/, 'special-command'],

        // Native блоки
        [/\bnative\b/, 'native-block'],
        [/\bendnative\b/, 'native-block'],

        // Слово "call"
        [/\bcall\b/, 'call-keyword'],

        // Обычные ключевые слова
        [/\b(if|else|endif|switch|for|endfor|while|endwhile|function|endfunc|print|Quit)\b/, 'keyword'],

        // Переменные
        [/[a-zA-Z_]\w*/, 'variable'],

        // Строки
        [/"(.*?)"/, 'string'],

        // Числа
        [/\d+\.\d+/, 'number.float'],
        [/\d+/, 'number'],

        // Комментарии
        [/\/\/.*/, 'comment'],
      ]
    }
  })

  // === SNENGINE ТЕМА (чёрно-красная) ===
  monaco.editor.defineTheme('snengine-dark', {
    base: 'vs-dark',
    inherit: true,
    rules: [
      { token: 'keyword', foreground: '#FF3B5C', fontStyle: 'bold' },           // Основные ключевые слова
      { token: 'special-command', foreground: '#FF1744', fontStyle: 'bold' },   // Show Character, Play Music и т.д.
      { token: 'call-keyword', foreground: '#FF5252', fontStyle: 'bold' },      // call
      { token: 'native-block', foreground: '#FF1744', fontStyle: 'bold' },      // native / endnative

      { token: 'variable', foreground: '#B0BEC5' },
      { token: 'string', foreground: '#FFCC80' },
      { token: 'number', foreground: '#FF8A80' },
      { token: 'number.float', foreground: '#FF8A80' },
      { token: 'comment', foreground: '#6D7278', fontStyle: 'italic' }
    ],
    colors: {
      'editor.background': '#1F1F1F',
      'editor.foreground': '#E0E0E0',
      'editorLineNumber.foreground': '#757575',
      'editorCursor.foreground': '#FF1744',
      'editor.selectionBackground': '#FF174440'
    }
  })
}