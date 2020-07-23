class ProductValidator {
    ValidateImageInput(imageContainer) {
        if (document.getElementById(imageContainer).value === "") {
            swal("Error", "Please select an image", "error");
            return false;
        }
        return true;
    }
}