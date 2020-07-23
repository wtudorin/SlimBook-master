class ProductEditScreenInitializer {
    Init() {
        tinymce.init({
            selector: 'textarea',
            plugins: 'lists',
            menubar: 'file edit format'
        });
    }
}

new ProductEditScreenInitializer().Init();