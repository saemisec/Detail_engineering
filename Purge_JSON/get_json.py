import os, json, sys

def build_tree(path):
    tree = {"Name": os.path.basename(path), "path": path}
    if os.path.isdir(path):
        tree["Type"] = "Folder"
        tree["Children"] = [build_tree(os.path.join(path, x)) for x in os.listdir(path)]
    else:
        tree["Type"] = "file"
    return tree

if __name__ == "__main__":
    root_path = r""
    result = build_tree(root_path)
    json_str = json.dumps(result['Children'], indent=2)
    with open("result.json", "w", encoding="utf-8") as out_stream:
        out_stream.write(json_str)
