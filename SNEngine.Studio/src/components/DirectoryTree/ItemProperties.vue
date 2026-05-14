<template>
  <FileProperties 
    v-if="file"
    :file="file"
    @close="close"
  />
</template>

<script setup lang="ts">
import { ref } from 'vue'
import FileProperties from '../FileProperties/FileProperties.vue'

const props = defineProps<{
  file: any // TreeItem
}>()

const emit = defineEmits<{
  (e: 'close'): void
}>()

const file = ref(props.file)

const close = () => {
  emit('close')
}
</script>