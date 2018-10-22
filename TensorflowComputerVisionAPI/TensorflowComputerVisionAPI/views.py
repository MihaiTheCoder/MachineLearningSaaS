"""
Routes and views for the flask application.
"""

from datetime import datetime
from flask import render_template
from TensorflowComputerVisionAPI import app, object_detection_runner
from flask import jsonify
from flask import Flask, request, send_from_directory, make_response
import os
from werkzeug.utils import secure_filename
import logging

log = logging.getLogger('TensorflowComputerVisionAPI')

@app.route('/')
@app.route('/home')
def home():
    #result = object_detection_runner.run_test_model_test_images()
    #print(result)
    """Renders the home page."""
    return render_template(
        'index.html',
        title='Home Page',
        year=datetime.now().year,
    )

@app.route('/detections')
def detections():
    print('start detections')
    result = object_detection_runner.run_test_model_test_images()
    print('detections done')
    return jsonify(result)

@app.route('/images/<path:path>')
def get_image(path):
    return send_from_directory(os.path.abspath(object_detection_runner.test_images_path), path)

def allowed_image(file_name):
    return file_name.endswith(('.jpg', '.png'))

def allowed_model_upload(file_name):
    return file_name.endswith(('.zip', '.tar.gz'))

def upload_model():
    file = request.files['file']
    
    if not allowed_model_upload(file.filename):
        return make_response(('for now you can only upload zip and tar.gz models', 400))

    filename = secure_filename(file.filename)
    trained_model_tar = os.path.join(object_detection_runner.trained_models_path, filename)
    current_chunk = int(request.form['dzchunkindex'])
    if os.path.exists(trained_model_tar) and current_chunk == 0:
        # 400 and 500s will tell dropzone that an error occurred and show an error
        return make_response(('File already exists', 400))
    try:
        with open(trained_model_tar, 'ab') as f:
            f.seek(int(request.form['dzchunkbyteoffset']))
            f.write(file.stream.read())            
    except :
        log.exception('Could not write to file')
        return make_response(("Not sure why,"
                              " but we couldn't write the file to disk", 500))

    total_chunks = int(request.form['dztotalchunkcount'])

    if current_chunk + 1 == total_chunks:
        if os.path.getsize(trained_model_tar) != int(request.form['dztotalfilesize']):
            log.error(f"File {file.filename} was completed, "
                      f"but has a size mismatch."
                      f"Was {os.path.getsize(trained_model_tar)} but we"
                      f" expected {request.form['dztotalfilesize']} ")
            return make_response(('Size mismatch', 500))
        else:
            log.info(f'File {file.filename} has been uploaded successfully')
            object_detection_runner.unzip_frozen_inference_graph(trained_model_tar, object_detection_runner.trained_models_path)
    else:
        log.debug(f'Chunk {current_chunk + 1} of {total_chunks} '
                  f'for file {file.filename} complete')
        

    return make_response(("Chunk upload successful", 200))

@app.route('/upload_model', methods=['GET', 'POST'])
def upload_model_api():
    if request.method == 'POST':
        return upload_model()
    return render_template('upload_model.html')

@app.route('/detect_image', methods=['GET', 'POST'])
def upload_file():
    if request.method == 'POST':
        # check if the post request has the file part
        if 'file' not in request.files:
            flash('No file part')
            return redirect(request.url)
        file = request.files['file']
        # if user does not select file, browser also
        # submit a empty part without filename
        if file.filename == '':
            flash('No selected file')
            return redirect(request.url)
        if file and allowed_image(file.filename):
            filename = secure_filename(file.filename)
            filepath = os.path.join(object_detection_runner.test_images_path, filename)
            file.save(filepath)
            #model_name = 'ssd_resnet50_v1_fpn_shared_box_predictor_640x640_coco14_sync_2018_07_03'
            #pbtxt_name = 'mscoco_label_map.pbtxt'            
            model_name = request.form['model']
            pbtxt_name = request.form['pbtxt']
            return jsonify(object_detection_runner.run_detection(model_name,pbtxt_name, filepath))

    models = object_detection_runner.get_models()
    pbtxt_files = object_detection_runner.get_pbtxt_files()
    return render_template('upload_image.html', title='Upload new File', models=models, pbtxt_files=pbtxt_files)

@app.route('/contact')
def contact():
    """Renders the contact page."""
    return render_template(
        'contact.html',
        title='Contact',
        year=datetime.now().year,
        message='Your contact page.'
    )

@app.route('/about')
def about():
    """Renders the about page."""
    return render_template(
        'about.html',
        title='About',
        year=datetime.now().year,
        message='Your application description page.'
    )
