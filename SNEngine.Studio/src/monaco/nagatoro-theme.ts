import * as monaco from 'monaco-editor'

export function registerNagatoroTheme() {
  monaco.editor.defineTheme('nagatoro-dark', {
    base: 'vs-dark',
    inherit: true,
    rules: [
      { token: 'keyword', foreground: '#FF6B6B', fontStyle: 'bold' },           // кораллово-розовый (фирменный цвет Нагаторо)
      { token: 'string', foreground: '#FFE4B5' },                              // тёплый крем/мокасин
      { token: 'number', foreground: '#FFD700' },                              // золотой
      { token: 'comment', foreground: '#8B7355', fontStyle: 'italic' },        // тёплый коричневый
      { token: 'function', foreground: '#FFA07A' },                            // светло-лососевый
      { token: 'variable', foreground: '#87CEEB' },                            // небесно-голубой (контраст)
      { token: 'type', foreground: '#FFB86C' },                                // тёплый оранжевый
      { token: 'tag', foreground: '#FF69B4', fontStyle: 'bold' },              // горячий розовый
      { token: 'attribute.name', foreground: '#FFD700' },                      // золотой
      { token: 'attribute.value', foreground: '#FFE4B5' },
      { token: 'selector', foreground: '#FF6B6B' },
      { token: 'property', foreground: '#87CEEB' },
      { token: 'special-command', foreground: '#FF1493', fontStyle: 'bold' },  // глубокий розовый
      { token: 'call-keyword', foreground: '#FFD700', fontStyle: 'bold' },
      { token: 'native-block', foreground: '#FF69B4', fontStyle: 'bold' }
    ],
    colors: {
      'editor.background': '#1A120B',           // тёплый тёмно-коричневый (загорелый вайб)
      'editor.foreground': '#F5E8D3',           // кремовый
      'editorLineNumber.foreground': '#8B7355',
      'editorCursor.foreground': '#FF69B4',
      'editor.selectionBackground': '#3D2B1F',
      'editor.selectionForeground': '#F5E8D3',
      'editor.findMatchBackground': '#FF69B4',
      'editor.findMatchHighlightBackground': '#FF69B440'
    }
  })
}