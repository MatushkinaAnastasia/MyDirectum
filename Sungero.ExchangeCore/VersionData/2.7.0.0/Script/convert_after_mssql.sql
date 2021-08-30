execute('update Sungero_ExCore_BoxBase set RootBox = Id where RootBox is Null')
execute('update Sungero_ExCore_QueueItem set RootBox = Box where RootBox is Null')