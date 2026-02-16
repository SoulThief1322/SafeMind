document.getElementById('imageInput').addEventListener('change', function () {
        const file = this.files[0];
        const label = document.getElementById('fileLabel');
        const previewContainer = document.getElementById('imagePreview');
        const previewImg = document.getElementById('previewImg');

        if (file) {
            label.textContent = file.name;
            const reader = new FileReader();
            reader.onload = function (e) {
                previewImg.src = e.target.result;
                previewContainer.style.display = 'block';
            };
            reader.readAsDataURL(file);
        } else {
            label.textContent = 'Choose an image…';
            previewContainer.style.display = 'none';
        }
    });