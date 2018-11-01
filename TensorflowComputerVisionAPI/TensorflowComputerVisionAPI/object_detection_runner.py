import numpy as np
import os
import six.moves.urllib as urllib
import sys
import tarfile
import tensorflow as tf
import zipfile

from distutils.version import StrictVersion
from collections import defaultdict
from io import StringIO
from matplotlib import pyplot as plt
from PIL import Image
import base64
import pickle
import codecs
import pandas as pd
import uuid
from os.path import isfile, join

project_path = "TensorflowComputerVisionAPI"
research_path = f"{project_path}/research-models/research"
object_detection_path = f"{research_path}/object_detection"

sys.path.append(research_path)
sys.path.append(object_detection_path)
from object_detection.utils import ops as utils_ops

from utils import label_map_util

from utils import visualization_utils as vis_util

if StrictVersion(tf.__version__) < StrictVersion('1.9.0'):
  raise ImportError('Please upgrade your TensorFlow installation to v1.9.* or later!')

pbtxt_dir_path = f"{object_detection_path}/data"
trained_models_path = f"{project_path}/trained_models"
standard_inference_graph_name = 'frozen_inference_graph.pb'
test_images_path = f"{object_detection_path}/test_images"
loaded_models = {}

if not os.path.exists(trained_models_path):
    os.makedirs(trained_models_path)


def run_detection(model_name, pbtxt_file, image_path):
    model = None
    category_index = None
    if model_name not in loaded_models:
        model_path = os.path.join(trained_models_path, model_name)
        model_path = os.path.join(model_path,[f for f in os.listdir(model_path) if f.endswith('.pb')][0])
        pbtxt_path = os.path.join(pbtxt_dir_path, pbtxt_file)
        model = load_model(model_path)
        category_index = load_categories(pbtxt_path)
        loaded_models[model_name] = (model, category_index)
    
    model, category_index = loaded_models[model_name]
    return run_detection_for_image(image_path, model, category_index)

def run_test_model_test_images():
    model_name, pbtxt_file = download_standard_model()
    images = get_images(test_images_path)
    detections = []
    for image_path in images:
        detections.append(run_detection(model_name, pbtxt_file, image_path))
    return detections


def download_standard_model():        
    """
    Download ssd mobilenet trained model from tensorflow
    Returns Path relative to trainedModels to frozen detection graph - This is the actual model that is used for the object detection 
    Returns also the pbtxt file name - List of the strings that is used to add correct label for each box."""
        
    # What model to download. 
    MODEL_NAME = 'ssd_mobilenet_v1_coco_2017_11_17'
    MODEL_FILE = MODEL_NAME + '.tar.gz'
    DOWNLOAD_BASE = 'http://download.tensorflow.org/models/object_detection/'
    model_path = os.path.join(trained_models_path, MODEL_NAME, standard_inference_graph_name)
    pbtxt_file = 'mscoco_label_map.pbtxt'
    pbtxt_path = os.path.join(pbtxt_dir_path, pbtxt_file)
    
    if os.path.exists(model_path):
        return MODEL_NAME, pbtxt_file

    trained_model_tar = os.path.join(trained_models_path, MODEL_FILE)
    
    if not os.path.exists(trained_model_tar):
        download_file(DOWNLOAD_BASE+MODEL_FILE, trained_model_tar)
    
    unzip_frozen_inference_graph(trained_model_tar, trained_models_path)
    return MODEL_NAME, pbtxt_file
        

def download_file(url, path):
    opener = urllib.request.urlretrieve(url, path)
        
def unzip_frozen_inference_graph(zip, directory):
    tar_file = tarfile.open(zip)
    for file in tar_file.getmembers():
        file_name = os.path.basename(file.name)
        if 'frozen_inference_graph.pb' in file_name:
            tar_file.extract(file, directory)
        elif file_name.endswith('.pb'):
            tar_file.extract(file, directory)

def load_model(model_path):
    detection_graph = tf.Graph()
    with detection_graph.as_default():
        od_graph_def = tf.GraphDef()
        with tf.gfile.GFile(model_path, 'rb') as fid:
            serialized_graph = fid.read()
            od_graph_def.ParseFromString(serialized_graph)
            tf.import_graph_def(od_graph_def, name='')            
    return detection_graph

def load_categories(pbtxt_path):
    """Label maps map indices to category names, so that when our convolution network predicts `5`, we know that this corresponds to `airplane`.  
    Here we use internal utility functions, but anything that returns a dictionary mapping integers to appropriate string labels would be fine"""
    return label_map_util.create_category_index_from_labelmap(pbtxt_path, use_display_name=True)

def load_image_into_numpy_array(image):
  (im_width, im_height) = image.size
  return np.array(image.getdata()).reshape(
      (im_height, im_width, 3)).astype(np.uint8)


def get_images(image_dir):
    return [ os.path.join(image_dir, 'image{}.jpg'.format(i)) for i in range(1, 3) ]


def run_inference_for_single_image(image, graph):
  with graph.as_default():
    with tf.Session() as sess:
      # Get handles to input and output tensors
      ops = tf.get_default_graph().get_operations()
      all_tensor_names = {output.name for op in ops for output in op.outputs}
      tensor_dict = {}
      for key in [
          'num_detections', 'detection_boxes', 'detection_scores',
          'detection_classes', 'detection_masks'
      ]:
        tensor_name = key + ':0'
        if tensor_name in all_tensor_names:
          tensor_dict[key] = tf.get_default_graph().get_tensor_by_name(
              tensor_name)
      if 'detection_masks' in tensor_dict:
        # The following processing is only for single image
        detection_boxes = tf.squeeze(tensor_dict['detection_boxes'], [0])
        detection_masks = tf.squeeze(tensor_dict['detection_masks'], [0])
        # Reframe is required to translate mask from box coordinates to image coordinates and fit the image size.
        real_num_detection = tf.cast(tensor_dict['num_detections'][0], tf.int32)
        detection_boxes = tf.slice(detection_boxes, [0, 0], [real_num_detection, -1])
        detection_masks = tf.slice(detection_masks, [0, 0, 0], [real_num_detection, -1, -1])
        detection_masks_reframed = utils_ops.reframe_box_masks_to_image_masks(
            detection_masks, detection_boxes, image.shape[0], image.shape[1])
        detection_masks_reframed = tf.cast(
            tf.greater(detection_masks_reframed, 0.5), tf.uint8)
        # Follow the convention by adding back the batch dimension
        tensor_dict['detection_masks'] = tf.expand_dims(
            detection_masks_reframed, 0)
      image_tensor = tf.get_default_graph().get_tensor_by_name('image_tensor:0')

      # Run inference
      output_dict = sess.run(tensor_dict,
                             feed_dict={image_tensor: np.expand_dims(image, 0)})

      # all outputs are float32 numpy arrays, so convert types as appropriate
      output_dict['num_detections'] = int(output_dict['num_detections'][0])
      output_dict['detection_classes'] = output_dict[
          'detection_classes'][0].astype(np.uint8)
      output_dict['detection_boxes'] = output_dict['detection_boxes'][0]
      output_dict['detection_scores'] = output_dict['detection_scores'][0]
      if 'detection_masks' in output_dict:
        output_dict['detection_masks'] = output_dict['detection_masks'][0]
  return output_dict

def run_detection_for_image(image_path, detection_graph, category_index):    
    image = Image.open(image_path)
    image_np = load_image_into_numpy_array(image)
    image_np_expanded = np.expand_dims(image_np, axis=0)
    output_dict = run_inference_for_single_image(image_np, detection_graph)
    vis_util.visualize_boxes_and_labels_on_image_array(
        image_np,
        output_dict['detection_boxes'],
        output_dict['detection_classes'],
        output_dict['detection_scores'],
        category_index,
        instance_masks=output_dict.get('detection_masks'),
        use_normalized_coordinates=True,
        line_thickness=8)
    unique_filename = f'{str(uuid.uuid4())}.jpg'
    result_path = os.path.join(test_images_path, unique_filename)
    Image.fromarray(image_np).save(result_path)
    coords_df =  get_coords(output_dict['detection_boxes'], output_dict['detection_scores'], output_dict['detection_classes'], category_index)
    result_dict = {'file': unique_filename, 'detection details': coords_df.to_json(orient='records')}
    return result_dict


def get_coords(boxes, scores, classes, category_index, min_accuracy=0.5):
    boxes_df = pd.DataFrame(boxes)
    scores_df = pd.DataFrame(scores)
    classes_df = pd.DataFrame(classes)
    classes_df = classes_df[0].map(category_index)
    boxes_scores_df = pd.concat([boxes_df, scores_df, classes_df], axis=1, ignore_index=True)
    boxes_scores_df.columns = ['ymin', 'xmin', 'ymax', 'xmax', 'scores', 'classes']
    boxes_scores_df = boxes_scores_df[boxes_scores_df.scores >= min_accuracy]

    return boxes_scores_df

def convert_numpy_arrays_from_dict(dict):
    dict_for_serialization = {}
    for key, value in dict.items():
        dict_for_serialization[key] = value if type(value) is not np.ndarray else encode_numpy_array(value)
    return dict_for_serialization


def encode_numpy_array(arr):
    return codecs.encode(pickle.dumps(arr), "base64").decode()

def save_model(file):    
    filename = secure_filename(file.filename)
    trained_model_tar = os.path.join(object_detection_runner.trained_models_path, filename)
    file.save(trained_model_tar)
    unzip_frozen_inference_graph(trained_model_tar, trained_models_path)

def get_models():
    directories = [dir for dir in os.listdir(trained_models_path) if not isfile(join(trained_models_path, dir))]
    return directories

def get_pbtxt_files():
    pbtxt_files = [file for file in os.listdir(pbtxt_dir_path) if file.endswith('.pbtxt')]
    return pbtxt_files

