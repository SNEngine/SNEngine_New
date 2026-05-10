import { ref } from 'vue'
import MessageBox from '../components/MessageBox/MessageBox.vue'

const messageBox = ref<InstanceType<typeof MessageBox> | null>(null)

export function useMessageBox() {
  const showMessageBox = (options: any) => {
    if (messageBox.value) {
      return messageBox.value.show(options)
    }
    return Promise.resolve('cancel')
  }

  return { showMessageBox, messageBox }
}