import { ref } from 'vue'
import InputBox from '../components/InputBox/InputBox.vue'

const inputBox = ref<InstanceType<typeof InputBox> | null>(null)

export function useInputBox() {
  const showInputBox = (options: any) => {
    if (inputBox.value) {
      return inputBox.value.show(options)
    }
    return Promise.resolve(null)
  }

  return { showInputBox, inputBox }
}