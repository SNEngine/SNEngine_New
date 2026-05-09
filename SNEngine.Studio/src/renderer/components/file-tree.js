export class FileTree {
    constructor(containerId, rootPath) {
        this.container = document.getElementById(containerId);
        this.rootPath = rootPath;
        this.render();
    }
    
    render() {
        this.container.innerHTML = `
            <div class="file-item" onclick="window.fileTree.openFolder('${this.rootPath}')">📁 ${this.rootPath}</div>
            <div style="padding-left: 16px;">
                <div class="file-item" onclick="window.fileTree.openFile('scripts/main.sn')">📄 main.sn</div>
                <div class="file-item" onclick="window.fileTree.openFile('characters/yuki.sn')">📄 yuki.sn</div>
                <div class="file-item" onclick="window.fileTree.openFile('backgrounds/street.sn')">📄 street.sn</div>
            </div>
        `;
    }
    
    openFolder(path) {
        console.log('Opening folder:', path);
    }
    
    openFile(filePath) {
        console.log('Opening file:', filePath);
        if (window.editorComponent) {
            window.editorComponent.loadFile(filePath);
        }
    }
}