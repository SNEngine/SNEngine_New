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

        // Слово "call" — отдельным цветом
        [/\bcall\b/, 'call-keyword'],

        // Остальные ключевые слова
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

  // === ТЕМА ===
  monaco.editor.defineTheme('sn-dark', {
    base: 'vs-dark',
    inherit: true,
    rules: [
      { token: 'keyword', foreground: '#C586C0', fontStyle: 'bold' },           // if, function, print и т.д.
      { token: 'call-keyword', foreground: '#FFCC00', fontStyle: 'bold' },      // call — ЖЁЛТЫЙ / ЗОЛОТОЙ
      { token: 'special-command', foreground: '#FF79C6', fontStyle: 'bold' },   // Show Character, Play Music и т.д.
      { token: 'native-block', foreground: '#FFCC00', fontStyle: 'bold' },      // native / endnative
      { token: 'variable', foreground: '#9CDCFE' },
      { token: 'string', foreground: '#CE9178' },
      { token: 'number', foreground: '#B5CEA8' },
      { token: 'comment', foreground: '#6A9955', fontStyle: 'italic' }
    ],
    colors: {
      'editor.background': '#1E1E1E',
      'editor.foreground': '#D4D4D4',
      'editorLineNumber.foreground': '#858585',
      'editorCursor.foreground': '#AEAFAD'
    }
  })
}